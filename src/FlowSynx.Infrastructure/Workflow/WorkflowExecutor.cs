using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.IO.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowExecutor : IWorkflowExecutor
{
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly ISystemClock _systemClock;
    private readonly IPluginTypeService _pluginTypeService;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IRetryService _retryService;
    private readonly ConcurrentDictionary<string, object?> _taskOutputs = new();

    public WorkflowExecutor(ILogger<WorkflowExecutor> logger, IWorkflowService workflowService,
        IWorkflowExecutionService workflowExecutionService, IWorkflowTaskExecutionService workflowTaskExecutionService,
        IJsonDeserializer jsonDeserializer, ISystemClock systemClock, IPluginTypeService pluginTypeService,
        IWorkflowValidator workflowValidator, IRetryService retryService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(workflowService);
        ArgumentNullException.ThrowIfNull(workflowExecutionService);
        ArgumentNullException.ThrowIfNull(workflowTaskExecutionService);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(pluginTypeService);
        ArgumentNullException.ThrowIfNull(workflowValidator);
        ArgumentNullException.ThrowIfNull(retryService);
        _logger = logger;
        _workflowService = workflowService;
        _workflowExecutionService = workflowExecutionService;
        _workflowTaskExecutionService = workflowTaskExecutionService;
        _jsonDeserializer = jsonDeserializer;
        _systemClock = systemClock;
        _pluginTypeService = pluginTypeService;
        _workflowValidator = workflowValidator;
        _retryService = retryService;
    }

    public async Task ExecuteAsync(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await GetWorkflow(userId, workflowId, cancellationToken);
        var workflowExecutionEntity = await InitilizeWorkflowExecution(workflow, cancellationToken);

        try
        {
            var deserializeWorkflow = DeserializeWorkflow(workflow.Definition);

            ValidateWorkflow(deserializeWorkflow.Tasks);

            var taskMap = deserializeWorkflow.Tasks.ToDictionary(t => t.Name);

            foreach (var item in taskMap)
            {
                await AddWorkflowTaskExecution(workflowExecutionEntity.Id, item.Key, cancellationToken);
            }

            var pendingTasks = new HashSet<string>(taskMap.Keys);

            while (pendingTasks.Any())
            {
                var readyTasks = pendingTasks
                    .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d)))
                    .ToList();

                if (!readyTasks.Any())
                    throw new InvalidOperationException("There are failed task in dependencies.");

                var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
                var errors = await ProcessWithDegreeOfParallelismAsync(userId, workflowExecutionEntity.Id, executionTasks,
                    deserializeWorkflow.Configuration, cancellationToken);

                if (errors.Any())
                {
                    var message = string.Join(Environment.NewLine, errors.Select(x=>x.Message));
                    _logger.LogError(message);
                    throw new Exception(message);
                }

                foreach (var taskId in readyTasks)
                    pendingTasks.Remove(taskId);
            }

            await ChangeWorkflowExecutionStatus(workflowExecutionEntity, WorkflowExecutionStatus.Completed, cancellationToken);
            _logger.LogInformation($"Workflow execution with Id: {workflow.Id} was completed successfully.");
        }
        catch (Exception ex)
        {
            await ChangeWorkflowExecutionStatus(workflowExecutionEntity, WorkflowExecutionStatus.Failed, cancellationToken);
            throw new Exception($"Workflow execution error: {ex.Message}");
        }
    }

    private WorkflowDefinition DeserializeWorkflow(string workFlowDefinition)
    {
        try
        {
            return _jsonDeserializer.Deserialize<WorkflowDefinition>(workFlowDefinition);
        }
        catch (JsonDeserializerException ex)
        {
            throw new Exception($"Json deserialization error: {ex.Message}");
        }
        catch (JsonReaderException ex)
        {
            throw new Exception($"Reader Error at Line {ex.LineNumber}, Position {ex.LinePosition}: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }

    private void ValidateWorkflow(List<WorkflowTask> workflowTasks)
    {
        var hasWorkflowPipelinesDuplicateNames = _workflowValidator.HasDuplicateNames(workflowTasks);
        if (hasWorkflowPipelinesDuplicateNames)
            throw new Exception("There is a duplicated pipeline name in the workflow pipelines.");

        var missingDependencies = _workflowValidator.AllDependenciesExist(workflowTasks);
        if (missingDependencies.Any())
        {
            var sb = new StringBuilder();
            sb.AppendLine("Invalid workflow: missing dependencies.. There are list of missing dependencies:");
            sb.AppendLine(string.Join(",", missingDependencies));
            throw new Exception(sb.ToString());
        }

        var validation = _workflowValidator.CheckCyclic(workflowTasks);
        if (validation.Cyclic)
        {
            var sb = new StringBuilder();
            sb.AppendLine("The workflow has cyclic dependencies. Please resolve them and try again!. There are Cyclic:");
            sb.AppendLine(string.Join(" -> ", validation.CyclicNodes));

            throw new Exception(sb.ToString());
        }
    }

    private async Task<WorkflowEntity> GetWorkflow(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        try
        {
            var workFlowEntity = await _workflowService.Get(userId, workflowId, cancellationToken);
            if (workFlowEntity == null)
                throw new Exception($"No workflow found with this Identity '{workflowId}'");

            return workFlowEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get Workflow failed. Meesage: {ex.ToString()}");
            throw new Exception(ex.ToString());
        }
    }

    private async Task<WorkflowExecutionEntity> InitilizeWorkflowExecution(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        try
        {
            var workflowExecutionEntity = await StartWorkflowExecution(workflowEntity.UserId, workflowEntity.Id, cancellationToken);
            return workflowExecutionEntity;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Initilize WorkflowExecution failed. Meesage: {ex.ToString()}");
            throw new Exception(ex.ToString());
        }
    }

    private async Task<WorkflowExecutionEntity> StartWorkflowExecution(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var executionEntity = new WorkflowExecutionEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            UserId = userId,
            ExecutionStart = _systemClock.UtcNow,
            Status = WorkflowExecutionStatus.Running,
            TaskExecutions = new List<WorkflowTaskExecutionEntity>()
        };
        await _workflowExecutionService.Add(executionEntity, cancellationToken);

        return executionEntity;
    }

    private async Task AddWorkflowTaskExecution(Guid workflowExecutionId, string name, CancellationToken cancellationToken)
    {
        var taskExecutionEntity = new WorkflowTaskExecutionEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            WorkflowExecutionId = workflowExecutionId,
            Status = WorkflowTaskExecutionStatus.Pending
        };
        await _workflowTaskExecutionService.Add(taskExecutionEntity, cancellationToken);
    }

    private async Task ChangeWorkflowExecutionStatus(WorkflowExecutionEntity workflowExecutionEntity, WorkflowExecutionStatus status, 
        CancellationToken cancellationToken)
    {
        workflowExecutionEntity.Status = status;
        workflowExecutionEntity.ExecutionEnd = _systemClock.UtcNow;
        await _workflowExecutionService.Update(workflowExecutionEntity, cancellationToken);
    }

    private async Task<List<Exception>> ProcessWithDegreeOfParallelismAsync(string userId, Guid workflowExecutionId, 
        IEnumerable<WorkflowTask> workflowTasks, WorkflowConfiguration workflowConfiguration, CancellationToken cancellationToken)
    {
        var degreeOfParallelism = workflowConfiguration.DegreeOfParallelism ?? 3;
        using var semaphore = new SemaphoreSlim(degreeOfParallelism);
        var exceptions = new List<Exception>();

        var parser = new ExpressionParser(_taskOutputs.ToDictionary());
        var tasks = workflowTasks.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ExecuteTaskAsync(userId, workflowExecutionId, item, workflowConfiguration.Retry, parser, cancellationToken);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return exceptions;
    }

    private async Task ExecuteTaskAsync(string userId, Guid workflowExecutionId, WorkflowTask task,
        WorkflowRetry? workflowRetry, ExpressionParser parser, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Executing task {task.Name}...");

            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name, 
                WorkflowTaskExecutionStatus.Running, cancellationToken);
            var executionResult = await TryExecuteAsync(userId, task, workflowRetry, parser, cancellationToken);
            _taskOutputs[task.Name] = executionResult;

            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name,
                            WorkflowTaskExecutionStatus.Completed, cancellationToken);
            _logger.LogInformation($"Task {task.Name} completed.");
        }
        catch (Exception ex)
        {
            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name,
                            WorkflowTaskExecutionStatus.Failed, cancellationToken);
            throw new Exception($"Task {task.Name} failed: {ex.Message}");
        }
    }

    private async Task ChangetWorkflowTaskExecutionStatus(Guid workflowExecutionId, string name, 
        WorkflowTaskExecutionStatus status, CancellationToken cancellationToken)
    {
        var workflowTaskExecutionEntity = await _workflowTaskExecutionService.Get(workflowExecutionId, name, cancellationToken);
        if (workflowTaskExecutionEntity == null)
            throw new Exception($"No workflow task execution found with name '{name}'");

        workflowTaskExecutionEntity.Status = status;
        workflowTaskExecutionEntity.EndTime = _systemClock.UtcNow;
        await _workflowTaskExecutionService.Update(workflowTaskExecutionEntity, cancellationToken);
    }

    private async Task<object?> TryExecuteAsync(string userId, WorkflowTask task, WorkflowRetry? workflowRetry,
        ExpressionParser parser, CancellationToken cancellationToken)
    {
        var plugin = await _pluginTypeService.Get(userId, task.Type, cancellationToken).ConfigureAwait(false);
        var resolvedParameters = task.Parameters;

        if (resolvedParameters != null && resolvedParameters.Any())
            ReplacePlaceholderInParameters(resolvedParameters, parser);
        
        var pluginParameters = resolvedParameters.ToPluginParameters();

        if (workflowRetry is null || workflowRetry.Max is <= 0)
        {
            return await plugin.ExecuteAsync(pluginParameters, cancellationToken).ConfigureAwait(false);
        }

        var maxRetries = workflowRetry.Max ?? 3;
        var delay = workflowRetry.Delay ?? 1000;

        return await _retryService.ExecuteAsync(
            async () => await plugin.ExecuteAsync(pluginParameters, cancellationToken).ConfigureAwait(false),
            maxRetries: maxRetries,
            delay: TimeSpan.FromMilliseconds(delay)
        );
    }

    private void ReplacePlaceholderInParameters(Dictionary<string, object?> parameters, ExpressionParser parser)
    {
        foreach (var key in new List<string>(parameters.Keys))
        {
            switch (parameters[key])
            {
                case Dictionary<string, object?> nestedDict:
                    ReplacePlaceholderInParameters(nestedDict, parser);
                    break;

                case List<string> stringList:
                    for (int i = 0; i < stringList.Count; i++)
                    {
                        stringList[i] = parser.Parse(stringList[i].ToString()) as string;
                    }
                    break;

                case JObject jObject:
                    ReplacePlaceholderInJObject(jObject, parser);
                    break;

                case JArray jArray:
                    ReplacePlaceholderInJArray(jArray, parser);
                    break;

                case string strValue:
                    if (IsJson(strValue))
                    {
                        var parsedJson = JsonConvert.DeserializeObject(strValue);
                        ReplacePlaceholderInJson(parsedJson, parser);
                        parameters[key] = JsonConvert.SerializeObject(parsedJson);
                    }
                    else
                    {
                        parameters[key] = parser.Parse(strValue);
                    }
                    break;
            }
        }
    }

    private void ReplacePlaceholderInJObject(JObject jObject, ExpressionParser parser)
    {
        foreach (var prop in jObject.Properties())
        {
            if (prop.Value.Type == JTokenType.String)
            {
                prop.Value = parser.Parse(prop.Value.ToString()) as string;
            }
            else if (prop.Value.Type == JTokenType.Object)
            {
                ReplacePlaceholderInJObject((JObject)prop.Value, parser);
            }
            else if (prop.Value.Type == JTokenType.Array)
            {
                ReplacePlaceholderInJArray((JArray)prop.Value, parser);
            }
        }
    }

    private void ReplacePlaceholderInJArray(JArray jArray, ExpressionParser parser)
    {
        for (int i = 0; i < jArray.Count; i++)
        {
            if (jArray[i].Type == JTokenType.String)
            {
                jArray[i] = parser.Parse(jArray[i].ToString()) as string;
            }
            else if (jArray[i].Type == JTokenType.Object)
            {
                ReplacePlaceholderInJObject((JObject)jArray[i], parser);
            }
            else if (jArray[i].Type == JTokenType.Array)
            {
                ReplacePlaceholderInJArray((JArray)jArray[i], parser);
            }
        }
    }

    private bool IsJson(string str)
    {
        str = str.Trim();
        return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
    }

    private void ReplacePlaceholderInJson(object? json, ExpressionParser parser)
    {
        if (json is JObject jObject)
        {
            ReplacePlaceholderInJObject(jObject, parser);
        }
        else if (json is JArray jArray)
        {
            ReplacePlaceholderInJArray(jArray, parser);
        }
    }
}