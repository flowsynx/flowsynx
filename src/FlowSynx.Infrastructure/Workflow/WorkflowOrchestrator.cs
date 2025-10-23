using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Serialization;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
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
    private readonly IWorkflowSchemaValidator _workflowSchemaValidator;
    private readonly IErrorHandlingResolver _errorHandlingResolver;
    private readonly IWorkflowCancellationRegistry _workflowCancellationRegistry;
    private readonly IManualApprovalService _manualApprovalService;
    private readonly IResultStorageProvider _resultStorageProvider;
    private readonly ILocalization _localization;
    private readonly IEventPublisher _eventPublisher;
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
        IWorkflowSchemaValidator workflowSchemaValidator,
        IErrorHandlingResolver errorHandlingResolver,
        IWorkflowCancellationRegistry workflowCancellationRegistry,
        IManualApprovalService manualApprovalService,
        IResultStorageFactory resultStorageFactory,
        ILocalization localization,
        IEventPublisher eventPublisher)
    {
        (_logger, _workflowService, _workflowExecutionService, _workflowTaskExecutionService,
         _taskExecutor, _parserFactory, _semaphoreFactory, _systemClock, _jsonDeserializer,
         _workflowValidator, _errorHandlingResolver, _workflowCancellationRegistry,
         _manualApprovalService, _localization, _eventPublisher) = (
            logger ?? throw new ArgumentNullException(nameof(logger)),
            workflowService ?? throw new ArgumentNullException(nameof(workflowService)),
            workflowExecutionService ?? throw new ArgumentNullException(nameof(workflowExecutionService)),
            workflowTaskExecutionService ?? throw new ArgumentNullException(nameof(workflowTaskExecutionService)),
            taskExecutor ?? throw new ArgumentNullException(nameof(taskExecutor)),
            parserFactory ?? throw new ArgumentNullException(nameof(parserFactory)),
            semaphoreFactory ?? throw new ArgumentNullException(nameof(semaphoreFactory)),
            systemClock ?? throw new ArgumentNullException(nameof(systemClock)),
            jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer)),
            workflowValidator ?? throw new ArgumentNullException(nameof(workflowValidator)),
            errorHandlingResolver ?? throw new ArgumentNullException(nameof(errorHandlingResolver)),
            workflowCancellationRegistry ?? throw new ArgumentNullException(nameof(workflowCancellationRegistry)),
            manualApprovalService ?? throw new ArgumentNullException(nameof(manualApprovalService)),
            localization ?? throw new ArgumentNullException(nameof(localization)),
            eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher))
        );

        _workflowSchemaValidator = workflowSchemaValidator ?? throw new ArgumentNullException(nameof(workflowSchemaValidator));

        _resultStorageProvider = resultStorageFactory?.GetDefaultProvider()
            ?? throw new ArgumentNullException(nameof(resultStorageFactory));
    }

    public async Task<WorkflowExecutionEntity> CreateWorkflowExecutionAsync(
        string userId,
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await FetchWorkflowOrThrowAsync(userId, workflowId, cancellationToken);
            var definition = await ParseAndValidateDefinitionAsync(
                workflow.Definition,
                workflow.SchemaUrl,
                cancellationToken);

            var execution = new WorkflowExecutionEntity
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                UserId = userId,
                WorkflowDefinition = workflow.Definition,
                WorkflowSchemaUrl = workflow.SchemaUrl,
                ExecutionStart = _systemClock.UtcNow,
                Status = WorkflowExecutionStatus.Pending,
                TaskExecutions = new List<WorkflowTaskExecutionEntity>()
            };
            await _workflowExecutionService.Add(execution, cancellationToken);
            await CreateTaskExecutionsAsync(workflowId, execution.Id, definition.Tasks, cancellationToken);

            var eventId = $"WorkflowExecutionUpdated-{workflowId}";
            var update = new
            {
                WorkflowId = workflowId,
                ExecutionId = execution.Id,
                ExecutionStart = execution.ExecutionStart,
                Status = execution.Status.ToString()
            };
            await _eventPublisher.PublishToUserAsync(userId, eventId, update, cancellationToken);

            _logger.LogInformation("Workflow execution {ExecutionId} created for workflow {WorkflowId}", execution.Id, workflowId);
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

    public async Task<WorkflowExecutionStatus> ExecuteWorkflowAsync(
        string userId, 
        Guid workflowId,
        Guid executionId,
        WorkflowTrigger? trigger,
        CancellationToken cancellationToken)
    {
        var execution = await _workflowExecutionService.Get(userId, workflowId, executionId, cancellationToken);
        if (execution == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionInitilizeFailed,
                _localization.Get("Workflow_Orchestrator_ExecutionNotFound", executionId));

        InitializeExecutionOutputs(trigger);

        var workflow = await FetchWorkflowOrThrowAsync(userId, workflowId, cancellationToken);
        var definition = await ParseAndValidateDefinitionAsync(
            execution.WorkflowDefinition,
            execution.WorkflowSchemaUrl ?? workflow.SchemaUrl,
            cancellationToken);

        await UpdateWorkflowAsRunningAsync(execution, cancellationToken);
        return await RunWorkflowExecutionAsync(userId, workflowId, definition, execution, cancellationToken);
    }

    public async Task<WorkflowExecutionStatus> ResumeWorkflowAsync(
        string userId, 
        Guid workflowId, 
        Guid executionId, 
        CancellationToken cancellationToken)
    {
        var execution = await _workflowExecutionService.Get(userId, workflowId, executionId, cancellationToken);
        if (execution == null || execution.Status != WorkflowExecutionStatus.Paused)
            throw new FlowSynxException((int)ErrorCode.WorkflowNotPaused,
                _localization.Get("Workflow_Orchestrator_WorkflowNotPaused", executionId));

        var workflow = await FetchWorkflowOrThrowAsync(userId, workflowId, cancellationToken);
        var definition = await ParseAndValidateDefinitionAsync(
            execution.WorkflowDefinition,
            execution.WorkflowSchemaUrl ?? workflow.SchemaUrl,
            cancellationToken);

        var executionContext = new WorkflowExecutionContext(userId, execution.WorkflowId, execution.Id);
        await LoadPreviousTaskResultsAsync(executionContext, cancellationToken);

        _logger.LogInformation("Resuming workflow '{WorkflowId}' from execution '{ExecutionId}'",
            execution.WorkflowId, execution.Id);

        return await RunWorkflowExecutionAsync(userId, workflowId, definition, execution, cancellationToken);
    }

    private async Task<WorkflowEntity> FetchWorkflowOrThrowAsync(
        string userId, 
        Guid workflowId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await _workflowService.Get(userId, workflowId, cancellationToken);
            return workflow ?? throw new FlowSynxException((int)ErrorCode.WorkflowNotFound,
                _localization.Get("Workflow_Orchestrator_WorkflowNotFound", workflowId));
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowGetItem,
                _localization.Get("Workflow_Executor_GetWorkflowFailed", ex.Message));
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task<WorkflowDefinition> ParseAndValidateDefinitionAsync(
        string definitionJson,
        string? schemaUrl,
        CancellationToken cancellationToken)
    {
        await _workflowSchemaValidator.ValidateAsync(schemaUrl, definitionJson, cancellationToken);
        var definition = _jsonDeserializer.Deserialize<WorkflowDefinition>(definitionJson);
        _errorHandlingResolver.Resolve(definition);
        _workflowValidator.Validate(definition);
        return definition;
    }

    private void InitializeExecutionOutputs(WorkflowTrigger? trigger)
    {
        _taskOutputs = new ConcurrentDictionary<string, object?>();

        if (trigger == null)
            return;

        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = trigger.Type.ToString(),
            ["properties"] = CloneProperties(trigger.Properties)
        };

        _taskOutputs.TryAdd("Trigger", metadata);

        if (trigger.Properties.TryGetValue("event", out var eventPayload))
        {
            _taskOutputs.TryAdd("TriggerEvent", eventPayload);
        }
    }

    private static Dictionary<string, object?> CloneProperties(IDictionary<string, object> source)
    {
        return source.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);
    }

    public async Task<WorkflowExecutionStatus> RunWorkflowExecutionAsync(
        string userId, 
        Guid workflowId, 
        WorkflowDefinition definition, 
        WorkflowExecutionEntity executionEntity, 
        CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope(CreateWorkflowLogScope(workflowId));
        using var __ = _logger.BeginScope(CreateWorkflowExecutionLogScope(executionEntity.Id));

        var registeredToken = _workflowCancellationRegistry.Register(userId, workflowId, executionEntity.Id);
        var taskMap = definition.Tasks.ToDictionary(t => t.Name);
        var pending = new HashSet<string>(taskMap.Keys);
        var executionContext = new WorkflowExecutionContext(userId, workflowId, executionEntity.Id);

        while (pending.Any())
        {
            var readyTasks = FindExecutableTasks(taskMap, pending);
            if (!readyTasks.Any())
                throw new FlowSynxException((int)ErrorCode.WorkflowFailedDependenciesTask,
                    _localization.Get("Workflow_Executor_FailedDependenciesTask"));

            var parser = _parserFactory.CreateParser(_taskOutputs.ToDictionary());
            var errors = new List<Exception>();

            foreach (var task in readyTasks.Select(t => taskMap[t]))
            {
                if (task.ManualApproval?.Enabled is true)
                {
                    var approvalStatus = await _manualApprovalService.GetApprovalStatusAsync(userId, workflowId, executionEntity.Id, task.Name, cancellationToken);

                    if (approvalStatus == WorkflowApprovalStatus.Rejected ||
                        approvalStatus == WorkflowApprovalStatus.TimedOut)
                    {
                        await UpdateWorkflowAsFailedAsync(executionEntity, cancellationToken);
                        return WorkflowExecutionStatus.Failed;
                    }

                    if (approvalStatus != WorkflowApprovalStatus.Approved)
                    {
                        _logger.LogInformation("Manual approval required for task '{TaskName}'", task.Name);
                        await PauseForManualApprovalAsync(userId, executionEntity, task, cancellationToken);
                        return WorkflowExecutionStatus.Paused;
                    }
                }

                var taskErrors = await ExecuteTasksInParallelAsync(executionContext, new[] { task },
                    parser, definition.Configuration, cancellationToken, registeredToken);
                errors.AddRange(taskErrors);
            }

            if (errors.Any())
            {
                await UpdateWorkflowAsFailedAsync(executionEntity, cancellationToken);
                ThrowIfAnyTaskFailed(errors);
            }

            foreach (var taskId in readyTasks)
                pending.Remove(taskId);
        }

        await UpdateWorkflowAsCompletedAsync(executionEntity, cancellationToken);
        _workflowCancellationRegistry.Remove(userId, workflowId, executionEntity.Id);
        return WorkflowExecutionStatus.Completed;
    }

    private async Task CreateTaskExecutionsAsync(
        Guid workflowId, 
        Guid executionId, 
        IEnumerable<WorkflowTask> tasks, 
        CancellationToken cancellationToken)
    {
        var executions = tasks.Select(task => new WorkflowTaskExecutionEntity
        {
            Id = Guid.NewGuid(),
            Name = task.Name,
            WorkflowId = workflowId,
            WorkflowExecutionId = executionId,
            Status = WorkflowTaskExecutionStatus.Pending
        });

        foreach (var execution in executions)
            await _workflowTaskExecutionService.Add(execution, cancellationToken);

        _logger.LogInformation("Initialized task executions for workflow '{WorkflowId}'", workflowId);
    }

    private async Task<List<Exception>> ExecuteTasksInParallelAsync(
        WorkflowExecutionContext context,
        IEnumerable<WorkflowTask> tasks,
        IExpressionParser parser,
        WorkflowConfiguration config,
        CancellationToken globalToken,
        CancellationToken localToken)
    {
        var errors = new ConcurrentBag<Exception>();
        var semaphore = _semaphoreFactory.Create(config.DegreeOfParallelism ?? 3);

        using var timeoutCts = new CancellationTokenSource(config.Timeout.HasValue
            ? TimeSpan.FromMilliseconds(config.Timeout.Value)
            : Timeout.InfiniteTimeSpan);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localToken, timeoutCts.Token);

        var executions = tasks.Select(async task =>
        {
            await semaphore.WaitAsync(linkedCts.Token);
            try
            {
                var result = await _taskExecutor.ExecuteAsync(context, task, parser, globalToken, linkedCts.Token);
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

    private static void ThrowIfAnyTaskFailed(IEnumerable<Exception> errors)
    {
        throw new FlowSynxException(new ErrorMessage(
            (int)ErrorCode.WorkflowTaskExecutionsList,
            string.Join(Environment.NewLine, errors.Select(e => e.Message))));
    }

    private async Task PauseForManualApprovalAsync(
        string userId, 
        WorkflowExecutionEntity execution, 
        WorkflowTask task, 
        CancellationToken cancellationToken)
    {
        var context = new WorkflowExecutionContext(userId, execution.WorkflowId, execution.Id);
        await PersistTaskResultsAsync(context, cancellationToken);

        execution.Status = WorkflowExecutionStatus.Paused;
        execution.PausedAtTask = task.Name;
        execution.ExecutionEnd = _systemClock.UtcNow;

        await _workflowExecutionService.Update(execution, cancellationToken);
        await _manualApprovalService.RequestApprovalAsync(execution, task.ManualApproval, cancellationToken);

        var update = new
        {
            WorkflowId = execution.WorkflowId,
            ExecutionId = execution.Id,
            ExecutionStart = execution.ExecutionStart,
            ExecutionEnd = execution.ExecutionEnd,
            Status = execution.Status.ToString()
        };
        await _eventPublisher.PublishToUserAsync(execution.UserId, "WorkflowExecutionUpdated", update, cancellationToken);

        _logger.LogInformation("Workflow '{WorkflowId}' paused for manual approval at task '{TaskName}'",
            execution.WorkflowId, task.Name);
    }

    private async Task PersistTaskResultsAsync(
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken)
    {
        if (_taskOutputs == null) return;

        await _resultStorageProvider.SaveResultAsync(context, _taskOutputs, cancellationToken);
        _logger.LogInformation("Saved result for workflow '{WorkflowExecutionId}' to storage", context.WorkflowExecutionId);
    }

    private async Task LoadPreviousTaskResultsAsync(
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken)
    {
        var result = await _resultStorageProvider.LoadResultAsync(context, cancellationToken);
        _taskOutputs = result ?? new ConcurrentDictionary<string, object?>();
        _logger.LogInformation("Result for workflow '{WorkflowExecutionId}' are restored", context.WorkflowExecutionId);
    }

    private async Task UpdateWorkflowAsRunningAsync(
        WorkflowExecutionEntity execution,
        CancellationToken cancellationToken)
    {
        execution.Status = WorkflowExecutionStatus.Running;
        await _workflowExecutionService.Update(execution, cancellationToken);

        var update = new
        {
            WorkflowId = execution.WorkflowId,
            ExecutionId = execution.Id,
            ExecutionStart = execution.ExecutionStart,
            Status = execution.Status.ToString()
        };
        await _eventPublisher.PublishToUserAsync(execution.UserId, "WorkflowExecutionUpdated", update, cancellationToken);
    }

    private async Task UpdateWorkflowAsFailedAsync(
        WorkflowExecutionEntity execution, 
        CancellationToken cancellationToken)
    {
        execution.ExecutionEnd = _systemClock.UtcNow;
        execution.Status = WorkflowExecutionStatus.Failed;
        await _workflowExecutionService.Update(execution, cancellationToken);

        var update = new
        {
            WorkflowId = execution.WorkflowId,
            ExecutionId = execution.Id,
            ExecutionStart = execution.ExecutionStart,
            ExecutionEnd = execution.ExecutionEnd,
            Status = execution.Status.ToString()
        };
        await _eventPublisher.PublishToUserAsync(execution.UserId, "WorkflowExecutionUpdated", update, cancellationToken);
    }

    private async Task UpdateWorkflowAsCompletedAsync(
        WorkflowExecutionEntity execution, 
        CancellationToken cancellationToken)
    {
        execution.ExecutionEnd = _systemClock.UtcNow;
        execution.Status = WorkflowExecutionStatus.Completed;
        await _workflowExecutionService.Update(execution, cancellationToken);
        var update = new
        {
            WorkflowId = execution.WorkflowId,
            ExecutionId = execution.Id,
            ExecutionStart = execution.ExecutionStart,
            ExecutionEnd = execution.ExecutionEnd,
            Status = execution.Status.ToString()
        };
        await _eventPublisher.PublishToUserAsync(execution.UserId, "WorkflowExecutionUpdated", update, cancellationToken);
    }

    private List<string> FindExecutableTasks(Dictionary<string, WorkflowTask> taskMap, HashSet<string> pending) =>
        pending.Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d))).ToList();

    private static LogScopeContext CreateWorkflowLogScope(Guid workflowId) => new() { { "WorkflowId", workflowId } };
    private static LogScopeContext CreateWorkflowExecutionLogScope(Guid workflowExecutionId) => new() { { "WorkflowExecutionId", workflowExecutionId } };
}
