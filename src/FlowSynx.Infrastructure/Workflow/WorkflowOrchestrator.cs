using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.ManualApprovals;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.Infrastructure.Workflow.ResultStorageProviders;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly IWorkflowTaskExecutor _taskExecutor;
    private readonly IExpressionParserFactory _parserFactory;
    private readonly ISemaphoreFactory _semaphoreFactory;
    private readonly ISystemClock _systemClock;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IWorkflowValidator _workflowValidator;
    private readonly IErrorHandlingResolver _errorHandlingResolver;
    private readonly IWorkflowCancellationRegistry _workflowCancellationRegistry;
    private readonly IManualApprovalService _manualApprovalService;
    private readonly IResultStorageProvider _resultStorageProvider;
    private readonly ILocalization _localization;
    private ConcurrentDictionary<string, object?> _taskOutputs = new();

    public WorkflowOrchestrator(
        ILogger<WorkflowOrchestrator> logger,
        IWorkflowService workflowService,
        IWorkflowExecutionService workflowExecutionService,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        IWorkflowTaskExecutor taskExecutor,
        IExpressionParserFactory parserFactory,
        ISemaphoreFactory semaphoreFactory,
        ISystemClock systemClock,
        IJsonDeserializer jsonDeserializer,
        IWorkflowValidator workflowValidator,
        IErrorHandlingResolver errorHandlingResolver,
        IWorkflowCancellationRegistry workflowCancellationRegistry,
        IManualApprovalService manualApprovalService,
        IResultStorageFactory resultStorageFactory,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _workflowExecutionService = workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService));
        _workflowTaskExecutionService = workflowTaskExecutionService ?? throw new ArgumentNullException(nameof(workflowTaskExecutionService));
        _taskExecutor = taskExecutor ?? throw new ArgumentNullException(nameof(taskExecutor));
        _parserFactory = parserFactory ?? throw new ArgumentNullException(nameof(parserFactory));
        _semaphoreFactory = semaphoreFactory ?? throw new ArgumentNullException(nameof(semaphoreFactory));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _jsonDeserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
        _workflowValidator = workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator));
        _errorHandlingResolver = errorHandlingResolver ?? throw new ArgumentNullException(nameof(errorHandlingResolver));
        _workflowCancellationRegistry = workflowCancellationRegistry ?? throw new ArgumentNullException(nameof(workflowCancellationRegistry));
        _manualApprovalService = manualApprovalService ?? throw new ArgumentNullException(nameof(manualApprovalService));
        _resultStorageProvider = resultStorageFactory.GetDefaultProvider() ?? throw new ArgumentNullException(nameof(resultStorageFactory));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization)); ;
    }

    public async Task<WorkflowExecutionStatus> ExecuteWorkflowAsync(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var workflowLogScopeContext = CreateWorkflowLogScope(workflowId);
        using (_logger.BeginScope(workflowLogScopeContext))
        {
            var workflow = await GetWorkflowAsync(userId, workflowId, cancellationToken);
            var definition = DeserializeAndValidate(workflow.Definition);

            var executionEntity = await StartWorkflowExecutionAsync(userId, workflowId, cancellationToken);
            var registeredToken = _workflowCancellationRegistry.Register(userId, workflowId, executionEntity.Id);

            await InitializeTaskExecutionsAsync(workflowId, executionEntity.Id, definition.Tasks, registeredToken);

            var taskMap = definition.Tasks.ToDictionary(t => t.Name);
            var pending = new HashSet<string>(taskMap.Keys);

            var executionContext = new WorkflowExecutionContext(userId, workflowId, executionEntity.Id);
            var workflowExecutionLogScopeContext = CreateWorkflowExecutionLogScope(executionEntity.Id);
            using (_logger.BeginScope(workflowExecutionLogScopeContext))
            {
                while (pending.Any())
                {
                    var readyTasks = GetReadyTasks(taskMap, pending);

                    if (!readyTasks.Any())
                        throw new FlowSynxException((int)ErrorCode.WorkflowFailedDependenciesTask,
                            _localization.Get("Workflow_Executor_FailedDependenciesTask"));

                    var parser = _parserFactory.CreateParser(_taskOutputs.ToDictionary());
                    var errors = new List<Exception>();

                    foreach (var task in readyTasks.Select(t => taskMap[t]))
                    {
                        // Check for manual approval
                        if (task.ManualApproval?.Enabled == true)
                        {
                            _logger.LogInformation("Manual approval required for task '{TaskName}'", task.Name);

                            // Pause the workflow and request approval
                            await PauseWorkflowForApprovalAsync(userId, executionEntity, task, cancellationToken);
                            return WorkflowExecutionStatus.Paused;
                        }

                        // Otherwise execute normally
                        var taskErrors = await ExecuteTaskBatchAsync(executionContext,
                            new[] { task }, parser, definition.Configuration, cancellationToken, registeredToken);

                        errors.AddRange(taskErrors);
                    }

                    if (errors.Any())
                    {
                        await MarkWorkflowAsFailedAsync(executionEntity, cancellationToken);
                        ThrowAggregatedTaskExceptions(errors);
                    }

                    foreach (var taskId in readyTasks)
                        pending.Remove(taskId);
                }

                await MarkWorkflowAsCompletedAsync(executionEntity, cancellationToken);
                _workflowCancellationRegistry.Remove(userId, workflowId, executionEntity.Id);
                return WorkflowExecutionStatus.Completed;
            }
        }
    }

    private async Task<WorkflowEntity> GetWorkflowAsync(
        string userId,
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _workflowService.Get(userId, workflowId, cancellationToken);
            return entity
                ?? throw new FlowSynxException((int)ErrorCode.WorkflowNotFound,
                _localization.Get("Workflow_Orchestrator_WorkflowNotFound", workflowId));
        }
        catch (Exception ex)
        {
            var messageMessage = new ErrorMessage((int)ErrorCode.WorkflowGetItem,
                _localization.Get("Workflow_Executor_GetWorkflowFailed", ex.Message));
            _logger.LogError(messageMessage.ToString());
            throw new FlowSynxException(messageMessage);
        }
    }

    private WorkflowDefinition DeserializeAndValidate(string definitionJson)
    {
        var definition = _jsonDeserializer.Deserialize<WorkflowDefinition>(definitionJson);
        _errorHandlingResolver.Resolve(definition);
        _workflowValidator.Validate(definition);
        return definition;
    }

    private async Task<WorkflowExecutionEntity> StartWorkflowExecutionAsync(
        string userId,
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var execution = new WorkflowExecutionEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            UserId = userId,
            ExecutionStart = _systemClock.UtcNow,
            Status = WorkflowExecutionStatus.Running,
            TaskExecutions = new List<WorkflowTaskExecutionEntity>()
        };

        try
        {
            await _workflowExecutionService.Add(execution, cancellationToken);
            _logger.LogInformation("Workflow '{WorkflowId}' started.", workflowId);
            return execution;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowExecutionInitilizeFailed,
                _localization.Get("Workflow_Executor_WorkflowInitilizeFailed", ex.Message));
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task InitializeTaskExecutionsAsync(
        Guid workflowId,
        Guid executionId,
        IEnumerable<WorkflowTask> tasks,
        CancellationToken cancellationToken)
    {
        foreach (var task in tasks)
        {
            var entity = new WorkflowTaskExecutionEntity
            {
                Id = Guid.NewGuid(),
                Name = task.Name,
                WorkflowId = workflowId,
                WorkflowExecutionId = executionId,
                Status = WorkflowTaskExecutionStatus.Pending
            };

            await _workflowTaskExecutionService.Add(entity, cancellationToken);
        }

        _logger.LogInformation("Initialized task executions for workflow '{WorkflowId}'", workflowId);
    }

    private async Task<List<Exception>> ExecuteTaskBatchAsync(
        WorkflowExecutionContext executionContext,
        IEnumerable<WorkflowTask> tasks,
        IExpressionParser parser,
        WorkflowConfiguration config,
        CancellationToken globalCancellationToken,
        CancellationToken cancellationToken)
    {
        var errors = new ConcurrentBag<Exception>();
        var semaphore = _semaphoreFactory.Create(config.DegreeOfParallelism ?? 3);
        using var globalCts = new CancellationTokenSource(config.Timeout.HasValue
            ? TimeSpan.FromMilliseconds(config.Timeout.Value)
            : Timeout.InfiniteTimeSpan);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, globalCts.Token);

        var executions = tasks.Select(async task =>
        {
            await semaphore.WaitAsync(linkedCts.Token);
            try
            {
                var result = await _taskExecutor.ExecuteAsync(executionContext, task,
                    parser, globalCancellationToken, linkedCts.Token);
                _taskOutputs[task.Name] = result;
            }
            catch (Exception ex)
            {
                errors.Add(new Exception(_localization.Get("WorkflowOrchestrator_TaskFailed", task.Name, ex.Message), ex));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(executions);
        return errors.ToList();
    }

    private static void ThrowAggregatedTaskExceptions(IEnumerable<Exception> errors)
    {
        throw new FlowSynxException(new ErrorMessage(
            (int)ErrorCode.WorkflowTaskExecutionsList,
            string.Join(Environment.NewLine, errors.Select(e => e.Message))));
    }

    private async Task MarkWorkflowAsFailedAsync(
        WorkflowExecutionEntity execution,
        CancellationToken cancellationToken)
    {
        execution.ExecutionEnd = _systemClock.UtcNow;
        execution.Status = WorkflowExecutionStatus.Failed;
        await _workflowExecutionService.Update(execution, cancellationToken);
    }

    private async Task MarkWorkflowAsCompletedAsync(
        WorkflowExecutionEntity execution,
        CancellationToken cancellationToken)
    {
        execution.ExecutionEnd = _systemClock.UtcNow;
        execution.Status = WorkflowExecutionStatus.Completed;
        await _workflowExecutionService.Update(execution, cancellationToken);
    }

    private List<string> GetReadyTasks(Dictionary<string, WorkflowTask> taskMap, HashSet<string> pending)
    {
        return pending
            .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d)))
            .ToList();
    }

    private static LogScopeContext CreateWorkflowLogScope(Guid workflowId) => new()
    {
        { "WorkflowId", workflowId }
    };

    private static LogScopeContext CreateWorkflowExecutionLogScope(Guid workflowExecutionId) => new()
    {
        { "WorkflowExecutionId", workflowExecutionId }
    };

    private async Task PauseWorkflowForApprovalAsync(
        string userId,
        WorkflowExecutionEntity execution,
        WorkflowTask task,
        CancellationToken cancellationToken)
    {
        var executionContext = new WorkflowExecutionContext(userId, execution.WorkflowId, execution.Id);
        await SaveTaskResultAsync(executionContext, cancellationToken);

        execution.Status = WorkflowExecutionStatus.Paused;
        execution.PausedAtTask = task.Name;
        execution.ExecutionEnd = _systemClock.UtcNow;

        await _workflowExecutionService.Update(execution, cancellationToken);

        // Send approval request
        await _manualApprovalService.RequestApprovalAsync(execution, task.ManualApproval, cancellationToken);

        _logger.LogInformation("Workflow '{WorkflowId}' paused for manual approval at task '{TaskName}'",
            execution.WorkflowId, task.Name);
    }

    public async Task<WorkflowExecutionStatus> ResumeWorkflowAsync(string userId, Guid executionId, Guid workflowId, CancellationToken cancellationToken)
    {
        var execution = await _workflowExecutionService.Get(userId, workflowId, executionId, cancellationToken);
        if (execution == null || execution.Status != WorkflowExecutionStatus.Paused)
            throw new FlowSynxException((int)ErrorCode.WorkflowNotPaused,
                _localization.Get("Workflow_Orchestrator_WorkflowNotPaused", executionId));

        var workflow = await GetWorkflowAsync(execution.UserId, execution.WorkflowId, cancellationToken);
        var definition = DeserializeAndValidate(workflow.Definition);
        var taskMap = definition.Tasks.ToDictionary(t => t.Name);

        var executionContext = new WorkflowExecutionContext(userId, execution.WorkflowId, execution.Id);
        await RestoreTaskResultsAsync(executionContext, cancellationToken);

        var pending = new HashSet<string>(taskMap.Keys.Where(t => !_taskOutputs.ContainsKey(t)));

        _logger.LogInformation("Resuming workflow '{WorkflowId}' from task '{TaskName}'",
            execution.WorkflowId, execution.PausedAtTask);

        return await ExecuteWorkflowAsync(execution.UserId, execution.WorkflowId, cancellationToken);
    }

    private async Task SaveTaskResultAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (_taskOutputs == null)
            return;

        await _resultStorageProvider.SaveResultAsync(
            context,
            _taskOutputs,
            cancellationToken);

        _logger.LogInformation(
            "Saved result for workflow '{WorkflowExecutionId}' to storage",
            context.WorkflowExecutionId);
    }

    private async Task RestoreTaskResultsAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken)
    {
        var result = await _resultStorageProvider.LoadResultAsync(context, cancellationToken);
        var outputs = result ?? new ConcurrentDictionary<string, object?>();

        _taskOutputs.Clear();
        _taskOutputs = outputs;

        _logger.LogInformation(
            "Result for workflow '{WorkflowExecutionId}' are restored",
            context.WorkflowExecutionId);
    }
}