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
    private readonly ITriggeredTaskQueue _triggeredTaskQueue;
    private ConcurrentDictionary<string, TaskOutput> _taskOutputs = new();

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
        IEventPublisher eventPublisher,
        ITriggeredTaskQueue triggeredTaskQueue)
    {
        (_logger, _workflowService, _workflowExecutionService, _workflowTaskExecutionService,
         _taskExecutor, _parserFactory, _semaphoreFactory, _systemClock, _jsonDeserializer,
         _workflowValidator, _errorHandlingResolver, _workflowCancellationRegistry,
         _manualApprovalService, _localization, _eventPublisher, _triggeredTaskQueue) = (
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
            eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher)),
            triggeredTaskQueue ?? throw new ArgumentNullException(nameof(triggeredTaskQueue))
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
        CancellationToken cancellationToken)
    {
        var execution = await _workflowExecutionService.Get(userId, workflowId, executionId, cancellationToken);
        if (execution == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionInitilizeFailed,
                _localization.Get("Workflow_Orchestrator_ExecutionNotFound", executionId));

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

        var conditionalTargets = definition.Tasks
            .Where(t => t.ConditionalBranches is { Count: > 0 })
            .SelectMany(t => t.ConditionalBranches.Select(b => b.TargetTaskName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pending = new HashSet<string>(
            taskMap.Keys.Where(k => !conditionalTargets.Contains(k)),
            StringComparer.OrdinalIgnoreCase
        );

        var context = new WorkflowExecutionContext(userId, workflowId, executionEntity.Id);

        var hadFailures = false;

        while (pending.Any() && !cancellationToken.IsCancellationRequested)
        {
            var readyTasks = GetReadyTasks(executionEntity.Id, taskMap, pending, conditionalTargets);
            if (!readyTasks.Any())
                return await HandleFailedDependenciesAsync(executionEntity, cancellationToken);

            var parserInputs = _taskOutputs.ToDictionary(kv => kv.Key, kv => kv.Value.Result);
            var parser = _parserFactory.CreateParser(parserInputs, definition.Variables);

            var approvedTasks = await GetApprovedTasksAsync(userId, workflowId, executionEntity, readyTasks, taskMap, cancellationToken);

            if (!approvedTasks.Any())
                return WorkflowExecutionStatus.Paused;

            var executionResults = await ExecuteApprovedTasksWithConditionalsAsync(
                context,
                approvedTasks,
                parser,
                definition.Configuration,
                cancellationToken,
                registeredToken);

            ProcessConditionalBranches(executionResults, taskMap, pending, parser, executionEntity.Id);

            if (executionResults.Errors.Any())
            {
                hadFailures = true;
                _logger.LogWarning("Errors occurred in parallel task execution: {Errors}",
                    string.Join(", ", executionResults.Errors.Select(e => e.Message)));
            }

            var completedTasks = executionResults.TaskResults.Values
                .Where(r => r.WasExecuted)
                .Select(r => r.TaskName)
                .ToList();

            UpdatePendingTasks(pending, completedTasks, executionEntity.Id);
        }

        if (hadFailures)
        {
            await UpdateWorkflowAsFailedAsync(executionEntity, cancellationToken);
            _workflowCancellationRegistry.Remove(userId, workflowId, executionEntity.Id);
            _triggeredTaskQueue.Clear(executionEntity.Id);
            return WorkflowExecutionStatus.Failed;
        }

        return await CompleteWorkflowAsync(userId, workflowId, executionEntity, cancellationToken);
    }

    private async Task<ConditionalExecutionResult> ExecuteApprovedTasksWithConditionalsAsync(
        WorkflowExecutionContext context,
        List<WorkflowTask> approvedTasks,
        IExpressionParser parser,
        WorkflowConfiguration config,
        CancellationToken globalToken,
        CancellationToken localToken)
    {
        var errors = new ConcurrentBag<Exception>();
        var conditionalResults = new ConcurrentDictionary<string, ConditionalTaskResult>();
        var semaphore = _semaphoreFactory.Create(config.DegreeOfParallelism ?? 3);

        using var timeoutCts = new CancellationTokenSource(config.Timeout.HasValue
            ? TimeSpan.FromMilliseconds(config.Timeout.Value)
            : Timeout.InfiniteTimeSpan);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localToken, timeoutCts.Token);

        var executions = approvedTasks.Select(async task =>
        {
            await semaphore.WaitAsync(linkedCts.Token);
            try
            {
                var shouldExecute = EvaluateTaskCondition(task, parser);

                if (!shouldExecute)
                {
                    _logger.LogInformation("Task '{TaskName}' skipped due to condition evaluation", task.Name);
                    conditionalResults[task.Name] = new ConditionalTaskResult
                    {
                        TaskName = task.Name,
                        WasExecuted = false,
                        WasConditionMet = false,
                        Output = TaskOutput.Success(null)
                    };
                    return;
                }

                var result = await _taskExecutor.ExecuteAsync(context, task, parser, globalToken, linkedCts.Token);
                _taskOutputs[task.Name] = result;

                conditionalResults[task.Name] = new ConditionalTaskResult
                {
                    TaskName = task.Name,
                    WasExecuted = true,
                    WasConditionMet = true,
                    Output = result
                };
            }
            catch (Exception ex)
            {
                _taskOutputs[task.Name] = TaskOutput.Failure(ex);
                errors.Add(new Exception(_localization.Get("WorkflowOrchestrator_TaskFailed", task.Name, ex.Message), ex));

                conditionalResults[task.Name] = new ConditionalTaskResult
                {
                    TaskName = task.Name,
                    WasExecuted = true,
                    WasConditionMet = true,
                    Output = TaskOutput.Failure(ex),
                    Error = ex
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(executions);

        return new ConditionalExecutionResult
        {
            Errors = errors.ToList(),
            TaskResults = conditionalResults
        };
    }

    private bool EvaluateTaskCondition(
        WorkflowTask task,
        IExpressionParser parser)
    {
        if (task.ExecutionCondition == null || string.IsNullOrWhiteSpace(task.ExecutionCondition.Expression))
            return true;

        try
        {
            var result = parser.Parse(task.ExecutionCondition.Expression);

            bool final = false;
            switch (result)
            {
                case bool b:
                    final = b;
                    break;
                case string s when bool.TryParse(s, out var sb):
                    final = sb;
                    break;
                default:
                    _logger.LogWarning("Execution condition '{Expression}' for task '{Task}' returned non-boolean result: {Value}",
                        task.ExecutionCondition.Expression, task.Name, result);
                    break;
            }

            _logger.LogDebug("Execution condition for task '{Task}': {Expression} = {Result}", task.Name, task.ExecutionCondition.Expression, final);
            return final;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating ExecutionCondition for task '{TaskName}': {Expression}", task.Name, task.ExecutionCondition.Expression);
            return false;
        }
    }


    private void ProcessConditionalBranches(
        ConditionalExecutionResult executionResults,
        Dictionary<string, WorkflowTask> taskMap,
        HashSet<string> pending,
        IExpressionParser parser,
        Guid executionId)
    {
        foreach (var taskResult in executionResults.TaskResults.Values)
        {
            if (!taskResult.WasExecuted || taskResult.Error != null)
                continue;

            if (!taskMap.TryGetValue(taskResult.TaskName, out var task))
                continue;

            if (task.ConditionalBranches is not { Count: > 0 })
                continue;

            foreach (var branch in task.ConditionalBranches)
            {
                try
                {
                    var condition = parser.Parse(branch.Expression);
                    bool takeBranch = false;

                    switch (condition)
                    {
                        case bool b:
                            takeBranch = b;
                            break;
                        case string s when bool.TryParse(s, out var sb):
                            takeBranch = sb;
                            break;
                    }

                    if (takeBranch)
                    {
                        if (!pending.Contains(branch.TargetTaskName))
                            pending.Add(branch.TargetTaskName);

                        _triggeredTaskQueue.Enqueue(executionId, branch.TargetTaskName);

                        _logger.LogInformation("Conditional branch taken from '{Source}' → '{Target}' (expr: {Expr})",
                            task.Name, branch.TargetTaskName, branch.Expression);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to evaluate conditional branch '{Expr}' from '{Source}'", branch.Expression, task.Name);
                }
            }
        }
    }

    private List<string> GetReadyTasks(
        Guid executionId,
        Dictionary<string, WorkflowTask> taskMap,
        HashSet<string> pending,
        HashSet<string> conditionalTargets)
    {
        var ready = FindExecutableTasks(executionId, taskMap, pending, conditionalTargets).ToList();
        var triggered = DequeueTriggeredTasks(executionId).ToList();

        foreach (var taskName in triggered)
            pending.Add(taskName);

        var all = ready.Union(triggered).Distinct().ToList();

        if (all.Count == 0)
            _logger.LogTrace("No tasks ready for execution at this time.");

        return all;
    }

    private async Task<List<WorkflowTask>> GetApprovedTasksAsync(
        string userId,
        Guid workflowId,
        WorkflowExecutionEntity executionEntity,
        IEnumerable<string> readyTasks,
        Dictionary<string, WorkflowTask> taskMap,
        CancellationToken cancellationToken)
    {
        var approvedTasks = new List<WorkflowTask>();

        foreach (var taskName in readyTasks)
        {
            var task = taskMap[taskName];
            if (task.ManualApproval?.Enabled == true)
            {
                var status = await _manualApprovalService.GetApprovalStatusAsync(
                    userId, 
                    workflowId, 
                    executionEntity.Id, 
                    task.Name, 
                    cancellationToken);

                var result = await HandleApprovalStatusAsync(
                    status, 
                    userId, 
                    executionEntity, 
                    task, 
                    cancellationToken);

                if (result == WorkflowExecutionStatus.Failed)
                    return new List<WorkflowTask>();
                if (result == WorkflowExecutionStatus.Paused)
                    return new List<WorkflowTask>();
                approvedTasks.Add(task);
            }
            else
            {
                approvedTasks.Add(task);
            }
        }

        return approvedTasks;
    }

    private async Task<WorkflowExecutionStatus> HandleApprovalStatusAsync(
        WorkflowApprovalStatus status,
        string userId,
        WorkflowExecutionEntity executionEntity,
        WorkflowTask task,
        CancellationToken cancellationToken)
    {
        switch (status)
        {
            case WorkflowApprovalStatus.Rejected:
            case WorkflowApprovalStatus.TimedOut:
                await UpdateWorkflowAsFailedAsync(executionEntity, cancellationToken);
                _triggeredTaskQueue.Clear(executionEntity.Id);
                return WorkflowExecutionStatus.Failed;

            case WorkflowApprovalStatus.Approved:
                return WorkflowExecutionStatus.Completed;

            default:
                _logger.LogInformation("Manual approval required for task '{TaskName}'", task.Name);
                await PauseForManualApprovalAsync(userId, executionEntity, task, cancellationToken);
                _triggeredTaskQueue.Clear(executionEntity.Id);
                return WorkflowExecutionStatus.Paused;
        }
    }

    private async Task<WorkflowExecutionStatus> HandleFailedDependenciesAsync(
        WorkflowExecutionEntity executionEntity,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Workflow execution failed due to unresolved dependencies.");
        await UpdateWorkflowAsFailedAsync(executionEntity, cancellationToken);
        _triggeredTaskQueue.Clear(executionEntity.Id);
        return WorkflowExecutionStatus.Failed;
    }

    private static void UpdatePendingTasks(HashSet<string> pending, IEnumerable<string> completed, Guid executionId)
    {
        foreach (var taskId in completed)
            pending.Remove(taskId);
    }

    private async Task<WorkflowExecutionStatus> CompleteWorkflowAsync(
        string userId,
        Guid workflowId,
        WorkflowExecutionEntity executionEntity,
        CancellationToken cancellationToken)
    {
        await UpdateWorkflowAsCompletedAsync(executionEntity, cancellationToken);
        _workflowCancellationRegistry.Remove(userId, workflowId, executionEntity.Id);
        _triggeredTaskQueue.Clear(executionEntity.Id);
        return WorkflowExecutionStatus.Completed;
    }

    private IEnumerable<string> DequeueTriggeredTasks(Guid executionId)
    {
        var list = new List<string>();
        while (_triggeredTaskQueue.TryDequeue(executionId, out var taskName))
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                _logger.LogInformation("Dequeued triggered task '{TaskName}' for execution.", taskName);
                list.Add(taskName);
            }
        }
        return list;
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

        var toPersist = new ConcurrentDictionary<string, object?>(
            _taskOutputs.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value))
        );

        await _resultStorageProvider.SaveResultAsync(context, toPersist, cancellationToken);
        _logger.LogInformation("Saved result for workflow '{WorkflowExecutionId}' to storage", context.WorkflowExecutionId);
    }

    private async Task LoadPreviousTaskResultsAsync(
        WorkflowExecutionContext context, 
        CancellationToken cancellationToken)
    {
        var result = await _resultStorageProvider.LoadResultAsync(context, cancellationToken);

        if (result == null)
        {
            _taskOutputs = new ConcurrentDictionary<string, TaskOutput>();
        }
        else
        {
            _taskOutputs = new ConcurrentDictionary<string, TaskOutput>(
                result.Select(kv => new KeyValuePair<string, TaskOutput>(kv.Key, MapToTaskOutput(kv.Value)))
            );
        }

        _logger.LogInformation("Result for workflow '{WorkflowExecutionId}' are restored", context.WorkflowExecutionId);
    }

    private static TaskOutput MapToTaskOutput(object? value)
    {
        if (value is TaskOutput to) return to;
        return TaskOutput.Success(value);
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

    private IEnumerable<string> FindExecutableTasks(
        Guid executionId,
        Dictionary<string, WorkflowTask> taskMap,
        HashSet<string> pending,
        HashSet<string> conditionalTargets)
    {
        var ready = new List<string>();

        var succeededTasks = _taskOutputs
            .Where(kv => kv.Value.Status == TaskOutputStatus.Succeeded)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failedTasks = _taskOutputs
            .Where(kv => kv.Value.Status == TaskOutputStatus.Failed)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var taskName in pending)
        {
            if (!taskMap.TryGetValue(taskName, out var task))
                continue;

            bool isConditionalTarget = conditionalTargets.Contains(taskName);
            bool isTriggered = _triggeredTaskQueue.Contains(executionId, taskName);

            if (isConditionalTarget && !isTriggered)
                continue;

            if (task.Dependencies == null || task.Dependencies.Count == 0)
            {
                ready.Add(taskName);
                continue;
            }

            bool depsSucceeded = task.Dependencies.All(dep => succeededTasks.Contains(dep));

            bool failureTriggered = task.RunOnFailureOf is { Count: > 0 } &&
                                    task.RunOnFailureOf.Any(f => failedTasks.Contains(f));

            if (depsSucceeded || failureTriggered)
                ready.Add(taskName);
        }

        return ready;
    }

    private static LogScopeContext CreateWorkflowLogScope(Guid workflowId) => new() { { "WorkflowId", workflowId } };
    private static LogScopeContext CreateWorkflowExecutionLogScope(Guid workflowExecutionId) => new() { { "WorkflowExecutionId", workflowExecutionId } };


    private sealed class ConditionalTaskResult
    {
        public string TaskName { get; set; } = string.Empty;
        public bool WasExecuted { get; set; }
        public bool WasConditionMet { get; set; }
        public TaskOutput Output { get; set; } = TaskOutput.Success(null);
        public Exception? Error { get; set; }
    }


    private sealed class ConditionalExecutionResult
    {
        public List<Exception> Errors { get; set; } = new();
        public ConcurrentDictionary<string, ConditionalTaskResult> TaskResults { get; set; } = new();
    }
}