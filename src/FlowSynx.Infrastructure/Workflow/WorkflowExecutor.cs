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
    private readonly ConcurrentDictionary<string, WorkflowTaskExecutionResult> _taskOutputs = new();

    public WorkflowExecutor(ILogger<WorkflowExecutor> logger, IWorkflowService workflowService,
        IWorkflowExecutionService workflowExecutionService, IWorkflowTaskExecutionService workflowTaskExecutionService,
        IJsonDeserializer jsonDeserializer, ISystemClock systemClock, IPluginTypeService pluginTypeService,
        IWorkflowValidator workflowValidator)
    {
        _logger = logger;
        _workflowService = workflowService;
        _workflowExecutionService = workflowExecutionService;
        _workflowTaskExecutionService = workflowTaskExecutionService;
        _jsonDeserializer = jsonDeserializer;
        _systemClock = systemClock;
        _pluginTypeService = pluginTypeService;
        _workflowValidator = workflowValidator;
    }

    public async Task<object?> ExecuteAsync(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await GetWorkflow(userId, workflowId, cancellationToken);
        var workflowExecutionEntity = await InitilizeWorkflowExecution(workflow, cancellationToken);

        try
        {
            var deserializeWorkflow = DeserializeWorkflow(workflow.Definition);

            var missingDependencies = _workflowValidator.AllDependenciesExist(deserializeWorkflow.Tasks);
            if (missingDependencies.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Invalid workflow: missing dependencies.. There are list of missing dependencies:");
                sb.AppendLine(string.Join(",", missingDependencies));
                throw new Exception(sb.ToString());
            }

            var validation = _workflowValidator.CheckCyclic(deserializeWorkflow.Tasks);
            if (validation.Cyclic)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The workflow has cyclic dependencies. Please resolve them and try again!. There are Cyclic:");
                sb.AppendLine(string.Join(" -> ", validation.CyclicNodes));

                throw new Exception(sb.ToString());
            }

            var taskMap = deserializeWorkflow.Tasks.ToDictionary(t => t.Name);

            foreach (var item in taskMap)
            {
                await AddWorkflowTaskExecution(workflowExecutionEntity.Id, item.Key, cancellationToken);
            }

            var pendingTasks = new HashSet<string>(taskMap.Keys);

            while (pendingTasks.Any())
            {
                var readyTasks = pendingTasks
                    .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d) && _taskOutputs[d].Status == WorkflowTaskStatus.Completed))
                    .ToList();

                if (!readyTasks.Any())
                    throw new InvalidOperationException("There are failed task in dependencies.");

                var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
                var errors = await ProcessWithDegreeOfParallelismAsync(userId, workflowExecutionEntity.Id, executionTasks,
                    deserializeWorkflow.Configuration.DegreeOfParallelism, cancellationToken);

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

            //var outputs = new Dictionary<string, object?>(_taskOutputs);
            return null;
        }
        catch (Exception ex)
        {
            await ChangeWorkflowExecutionStatus(workflowExecutionEntity, WorkflowExecutionStatus.Failed, cancellationToken);
            _logger.LogError($"Workflow execution error: {ex.Message}");
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
            ExecutionStart = _systemClock.NowUtc,
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
        workflowExecutionEntity.ExecutionEnd = _systemClock.NowUtc;
        await _workflowExecutionService.Update(workflowExecutionEntity, cancellationToken);
    }

    private async Task<List<Exception>> ProcessWithDegreeOfParallelismAsync(string userId, Guid workflowExecutionId, IEnumerable<WorkflowTask> workflowTasks, 
        int degreeOfParallelism, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(degreeOfParallelism);
        var exceptions = new List<Exception>();

        var tasks = workflowTasks.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ExecuteTaskAsync(userId, workflowExecutionId, item, cancellationToken);
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

    private async Task ExecuteTaskAsync(string userId, Guid workflowExecutionId, WorkflowTask task, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Executing task {task.Name}...");

            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name, 
                WorkflowTaskExecutionStatus.Running, cancellationToken);

            _taskOutputs[task.Name] = new WorkflowTaskExecutionResult
            {
                Status = WorkflowTaskStatus.Completed
            };

            var executionResult = await TryExecuteAsync(userId, task, cancellationToken);
            _taskOutputs[task.Name] = new WorkflowTaskExecutionResult { 
                Result = executionResult, 
                Status = WorkflowTaskStatus.Completed 
            };

            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name,
                            WorkflowTaskExecutionStatus.Completed, cancellationToken);
            //_logger.LogInformation($"Task {task.Name} completed.");
        }
        catch (Exception ex)
        {
            await ChangetWorkflowTaskExecutionStatus(workflowExecutionId, task.Name,
                            WorkflowTaskExecutionStatus.Failed, cancellationToken);
            _taskOutputs[task.Name] = new WorkflowTaskExecutionResult
            {
                Status = WorkflowTaskStatus.Failed
            };
            //_logger.LogError($"Task {task.Name} failed: {ex.Message}");
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
        workflowTaskExecutionEntity.EndTime = _systemClock.NowUtc;
        await _workflowTaskExecutionService.Update(workflowTaskExecutionEntity, cancellationToken);
    }

    private async Task<object?> TryExecuteAsync(string userId, WorkflowTask task, 
        CancellationToken cancellationToken)
    {
        var plugin = await _pluginTypeService.Get(userId, task.Type, cancellationToken).ConfigureAwait(false);
        var pluginParameters = task.Parameters.ToPluginParameters();
        return await plugin.ExecuteAsync(pluginParameters, cancellationToken).ConfigureAwait(false);
    }

    private void ConvertBooleansToLowercase(JObject jObject)
    {
        foreach (var property in jObject.Properties().ToList())
        {
            if (property.Value.Type == JTokenType.Boolean)
            {
                // Convert the boolean value to lowercase "true" or "false"
                property.Value = property.Value.ToString().ToLower();
            }
            else if (property.Value.Type == JTokenType.Object)
            {
                // Recursively handle nested objects
                ConvertBooleansToLowercase((JObject)property.Value);
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                // Handle nested arrays if needed
                foreach (var item in property.Value)
                {
                    if (item.Type == JTokenType.Boolean)
                    {
                        item.Replace(item.ToString().ToLower());
                    }
                }
            }
        }
    }
}