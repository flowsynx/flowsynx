using FlowSynx.Application.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.Expressions;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTaskExecutor : IWorkflowTaskExecutor
{
    private readonly ILogger<WorkflowTaskExecutor> _logger;
    private readonly IPluginTypeService _pluginTypeService;
    private readonly IPlaceholderReplacer _placeholderReplacer;
    private readonly IErrorHandlingStrategyFactory _errorHandlingStrategyFactory;
    private readonly IWorkflowTaskExecutionService _workflowTaskExecutionService;
    private readonly ISystemClock _systemClock;
    private readonly ILocalization _localization;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITriggeredTaskQueue _triggeredTaskQueue;
    private readonly IAgentExecutor? _agentExecutor;

    public WorkflowTaskExecutor(
        ILogger<WorkflowTaskExecutor> logger,
        IPluginTypeService pluginTypeService,
        IPlaceholderReplacer placeholderReplacer,
        IErrorHandlingStrategyFactory errorHandlingStrategyFactory,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ISystemClock systemClock,
        ILocalization localization,
        IEventPublisher eventPublisher,
        ITriggeredTaskQueue triggeredTaskQueue,
        IAgentExecutor? agentExecutor = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginTypeService = pluginTypeService ?? throw new ArgumentNullException(nameof(pluginTypeService));
        _placeholderReplacer = placeholderReplacer ?? throw new ArgumentNullException(nameof(placeholderReplacer));
        _errorHandlingStrategyFactory = errorHandlingStrategyFactory ?? throw new ArgumentNullException(nameof(errorHandlingStrategyFactory));
        _workflowTaskExecutionService = workflowTaskExecutionService ?? throw new ArgumentNullException(nameof(workflowTaskExecutionService));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _triggeredTaskQueue = triggeredTaskQueue ?? throw new ArgumentNullException(nameof(triggeredTaskQueue));
        _agentExecutor = agentExecutor;
    }

    public async Task<TaskOutput> ExecuteAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task,
        IExpressionParser expressionParser,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken)
    {
        var taskExecution = await _workflowTaskExecutionService.Get(
            executionContext.WorkflowId, 
            executionContext.WorkflowExecutionId, 
            task.Name, 
            globalCancellationToken);

        if (taskExecution == null)
        {
            _logger.LogError("Workflow task '{TaskName}' is not initialized.", task.Name);
            throw new FlowSynxException((int)ErrorCode.WorkflowGetTaskExecutionItem,
                $"Workflow task '{task.Name}' is not initialized.");
        }

        var logScopeContext = CreateLogScope(taskExecution.Id, task.Name);
        using (_logger.BeginScope(logScopeContext))
        {
            taskExecution.StartTime = _systemClock.UtcNow;
            await UpdateTaskStatusAsync(executionContext.UserId, taskExecution, WorkflowTaskExecutionStatus.Running, globalCancellationToken);
            _logger.LogInformation("Workflow task '{TaskName}' started.", task.Name);
            var context = new ErrorHandlingContext { TaskName = task.Name, RetryCount = 0 };
            return await ExecuteTaskAsync(executionContext, task, taskExecution, expressionParser, context, 
                globalCancellationToken, taskCancellationToken);
        }
    }

    private async Task<TaskOutput> ExecuteTaskAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task,
        WorkflowTaskExecutionEntity taskExecution,
        IExpressionParser expressionParser,
        ErrorHandlingContext errorContext,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken)
    {
        using var timeoutCts = CreateTimeoutToken(task.TimeoutMilliseconds, taskCancellationToken);
        var token = timeoutCts.Token;

        await ParseWorkflowTaskPlaceholders(task, expressionParser, token);
        await ProcessOutputResults(executionContext.TaskOutputs, expressionParser, token);

        // Check if agent execution is enabled for this task
        if (task.Agent?.Enabled == true && _agentExecutor != null)
        {
            return await ExecuteWithAgentAsync(
                executionContext,
                task,
                taskExecution,
                expressionParser,
                errorContext,
                globalCancellationToken,
                token);
        }

        // Standard plugin execution
        return await ExecuteWithPluginAsync(
            executionContext,
            task,
            taskExecution,
            expressionParser,
            errorContext,
            globalCancellationToken,
            token);
    }

    /// <summary>
    /// Executes task using AI agent with optional plugin assistance.
    /// </summary>
    private async Task<TaskOutput> ExecuteWithAgentAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task,
        WorkflowTaskExecutionEntity taskExecution,
        IExpressionParser expressionParser,
        ErrorHandlingContext errorContext,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken)
    {
        _logger.LogInformation(
            "Executing task '{TaskName}' with AI agent in mode '{Mode}'",
            task.Name,
            task.Agent!.Mode);

        var retryStrategy = _errorHandlingStrategyFactory.Create(task.ErrorHandling);

        try
        {
            taskCancellationToken.ThrowIfCancellationRequested();

            // Build agent context
            var agentContext = new AgentExecutionContext
            {
                TaskName = task.Name,
                TaskType = task.Type,
                TaskDescription = task.Description,
                TaskParameters = task.Parameters,
                WorkflowVariables = executionContext.WorkflowVariables,
                PreviousTaskOutputs = executionContext.TaskOutputs,
                UserInstructions = task.Agent.Instructions,
                AdditionalContext = task.Agent.Context
            };

            // Execute with agent
            var agentResult = await _agentExecutor!.ExecuteAsync(
                agentContext,
                task.Agent,
                taskCancellationToken);

            // Log agent reasoning and steps
            if (!string.IsNullOrWhiteSpace(agentResult.Reasoning))
            {
                _logger.LogInformation(
                    "Agent reasoning for '{TaskName}': {Reasoning}",
                    task.Name,
                    agentResult.Reasoning);
            }

            foreach (var step in agentResult.Steps)
            {
                _logger.LogDebug("Agent step: {Step}", step);
            }

            if (!agentResult.Success)
            {
                throw new FlowSynxException(
                    (int)ErrorCode.AIAgentExecutionFailed,
                    $"Agent execution failed: {agentResult.ErrorMessage}");
            }

            // If agent mode is "assist", still execute the plugin with agent guidance
            if (task.Agent.Mode == "assist")
            {
                _logger.LogInformation(
                    "Agent provided guidance, proceeding with plugin execution for '{TaskName}'",
                    task.Name);
                
                return await ExecuteWithPluginAsync(
                    executionContext,
                    task,
                    taskExecution,
                    expressionParser,
                    errorContext,
                    globalCancellationToken,
                    taskCancellationToken);
            }

            // For execute/plan/validate modes, use agent output directly
            var output = ResolvePluginOutput(agentResult.Output, task);

            await CompleteTaskWithLogAsync(
                executionContext.UserId,
                taskExecution,
                WorkflowTaskExecutionStatus.Completed,
                globalCancellationToken,
                LogLevel.Information,
                "Workflow task '{TaskName}' completed with agent.",
                task.Name);

            return TaskOutput.Success(output);
        }
        catch (OperationCanceledException) when (taskCancellationToken.IsCancellationRequested || globalCancellationToken.IsCancellationRequested)
        {
            var reason = globalCancellationToken.IsCancellationRequested
                ? WorkflowTaskCancellationReason.Workflow
                : WorkflowTaskCancellationReason.Timeout;

            await HandleCancellationAndThrowAsync(executionContext.UserId, taskExecution, task.Name, reason, globalCancellationToken);
        }
        catch (Exception ex)
        {
            if (taskCancellationToken.IsCancellationRequested)
            {
                await HandleCancellationAndThrowAsync(
                    executionContext.UserId,
                    taskExecution,
                    task.Name,
                    WorkflowTaskCancellationReason.TaskToken,
                    globalCancellationToken);
            }

            var result = await retryStrategy.HandleAsync(errorContext, taskCancellationToken);
            TriggerTaskIfNeeded(taskExecution, task.Name, result);

            if (result?.ShouldRetry == true)
            {
                await UpdateTaskStatusAsync(executionContext.UserId, taskExecution, WorkflowTaskExecutionStatus.Retrying, globalCancellationToken);
                _logger.LogWarning(ex, "Workflow task '{TaskName}' retrying with agent.", task.Name);
                return await ExecuteTaskAsync(executionContext, task, taskExecution, expressionParser, errorContext, globalCancellationToken, taskCancellationToken);
            }

            if (result?.ShouldSkip == true)
            {
                await CompleteTaskWithLogAsync(
                    executionContext.UserId,
                    taskExecution,
                    WorkflowTaskExecutionStatus.Completed,
                    globalCancellationToken,
                    LogLevel.Warning,
                    "Workflow task '{TaskName}' skipped after agent execution.",
                    task.Name);

                return TaskOutput.Success(null);
            }

            await FailTaskAsync(executionContext.UserId, taskExecution, ex, globalCancellationToken, task.Name);
        }

        return TaskOutput.Success(null);
    }

    /// <summary>
    /// Standard plugin-based execution without agent.
    /// </summary>
    private async Task<TaskOutput> ExecuteWithPluginAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task,
        WorkflowTaskExecutionEntity taskExecution,
        IExpressionParser expressionParser,
        ErrorHandlingContext errorContext,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken)
    {
        var userId = executionContext.UserId;
        var plugin = await _pluginTypeService.Get(userId, task.Type, taskCancellationToken);
        var pluginParameters = task.Parameters.ToPluginParameters();
        var retryStrategy = _errorHandlingStrategyFactory.Create(task.ErrorHandling);

        try
        {
            taskCancellationToken.ThrowIfCancellationRequested();
            var executionResult = await plugin.ExecuteAsync(pluginParameters, taskCancellationToken);
            var output = ResolvePluginOutput(executionResult, task);

            await CompleteTaskWithLogAsync(
                userId,
                taskExecution,
                WorkflowTaskExecutionStatus.Completed,
                globalCancellationToken,
                LogLevel.Information,
                "Workflow task '{TaskName}' completed.",
                task.Name);

            return TaskOutput.Success(output);
        }
        catch (OperationCanceledException) when (taskCancellationToken.IsCancellationRequested || globalCancellationToken.IsCancellationRequested)
        {
            var reason = globalCancellationToken.IsCancellationRequested
                ? WorkflowTaskCancellationReason.Workflow
                : WorkflowTaskCancellationReason.Timeout;

            await HandleCancellationAndThrowAsync(userId, taskExecution, task.Name, reason, globalCancellationToken);
        }
        catch (Exception ex)
        {
            if (taskCancellationToken.IsCancellationRequested)
            {
                await HandleCancellationAndThrowAsync(
                    userId,
                    taskExecution,
                    task.Name,
                    WorkflowTaskCancellationReason.TaskToken,
                    globalCancellationToken);
            }

            var result = await retryStrategy.HandleAsync(errorContext, taskCancellationToken);
            TriggerTaskIfNeeded(taskExecution, task.Name, result);

            if (result?.ShouldRetry == true)
            {
                await UpdateTaskStatusAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Retrying, globalCancellationToken);
                _logger.LogWarning("Workflow task '{TaskName}' retrying.", task.Name);
                return await ExecuteTaskAsync(executionContext, task, taskExecution, expressionParser, errorContext, globalCancellationToken, taskCancellationToken);
            }

            if (result?.ShouldSkip == true)
            {
                await CompleteTaskWithLogAsync(
                    userId,
                    taskExecution,
                    WorkflowTaskExecutionStatus.Completed,
                    globalCancellationToken,
                    LogLevel.Warning,
                    "Workflow task '{TaskName}' skipped.",
                    task.Name);

                return TaskOutput.Success(null);
            }

            await FailTaskAsync(userId, taskExecution, ex, globalCancellationToken, task.Name);
        }

        return TaskOutput.Success(null);
    }

    /// <summary>
    /// Normalizes plugin output by falling back to the configured task output when a plugin returns null.
    /// </summary>
    private static object? ResolvePluginOutput(object? pluginResult, WorkflowTask task)
    {
        if (pluginResult is not null || string.IsNullOrEmpty(task.Output))
            return pluginResult;

        return new PluginContext(task.Name, "Data") { Content = task.Output };
    }

    /// <summary>
    /// Completes the task and logs with a consistent message template.
    /// </summary>
    private async Task CompleteTaskWithLogAsync(
        string userId,
        WorkflowTaskExecutionEntity taskExecution,
        WorkflowTaskExecutionStatus status,
        CancellationToken cancellationToken,
        LogLevel logLevel,
        string messageTemplate,
        string taskName)
    {
        await CompleteTaskAsync(userId, taskExecution, status, cancellationToken);
        _logger.Log(logLevel, messageTemplate, taskName);
    }

    /// <summary>
    /// Handles cancellation flows by completing the task, logging, and throwing the mapped exception.
    /// </summary>
    private async Task HandleCancellationAndThrowAsync(
        string userId,
        WorkflowTaskExecutionEntity taskExecution,
        string taskName,
        WorkflowTaskCancellationReason reason,
        CancellationToken cancellationToken)
    {
        await CompleteTaskWithLogAsync(
            userId,
            taskExecution,
            WorkflowTaskExecutionStatus.Canceled,
            cancellationToken,
            LogLevel.Error,
            "Workflow task '{TaskName}' canceled.",
            taskName);

        var exception = reason == WorkflowTaskCancellationReason.Workflow
            ? new FlowSynxException((int)ErrorCode.WorkflowExecutionCanceled,
                _localization.Get("RetryService_WorkflowCanceled", taskName, 0))
            : new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionCanceled,
                _localization.Get("RetryService_TaskCanceled", taskName, 0));

        throw exception;
    }

    /// <summary>
    /// Enqueues follow-up tasks when the retry strategy requests it.
    /// </summary>
    private void TriggerTaskIfNeeded(WorkflowTaskExecutionEntity taskExecution, string taskName, ErrorHandlingResult? result)
    {
        if (result?.ShouldTriggerTask != true || string.IsNullOrWhiteSpace(result.TaskToTrigger))
            return;

        _triggeredTaskQueue.Enqueue(taskExecution.WorkflowExecutionId, result.TaskToTrigger);
        _logger.LogInformation("Triggered task '{TaskToTrigger}' due to error in '{TaskName}'.", result.TaskToTrigger, taskName);
    }

    /// <summary>
    /// Distinguishes the origin of task cancellation for accurate error reporting.
    /// </summary>
    private enum WorkflowTaskCancellationReason
    {
        Timeout,
        Workflow,
        TaskToken
    }

    private async Task FailTaskAsync(
        string userId,
        WorkflowTaskExecutionEntity entity,
        Exception ex,
        CancellationToken cancellationToken,
        string taskName)
    {
        await CompleteTaskAsync(userId, entity, WorkflowTaskExecutionStatus.Failed, cancellationToken);
        _logger.LogError(ex, "Workflow task '{TaskName}' failed: {Message}", taskName, ex.Message);
        throw new Exception(ex.Message, ex);
    }

    private async Task UpdateTaskStatusAsync(
        string userId,
        WorkflowTaskExecutionEntity entity,
        WorkflowTaskExecutionStatus status,
        CancellationToken cancellationToken)
    {
        entity.Status = status;
        await _workflowTaskExecutionService.Update(entity, cancellationToken);

        var eventId = $"WorkflowTaskExecutionUpdated-{entity.WorkflowId}-{entity.WorkflowExecutionId}";
        var updateTask = new
        {
            WorkflowId = entity.WorkflowId,
            ExecutionId = entity.WorkflowExecutionId,
            TaskId = entity.Id,
            TaskName = entity.Name,
            Status = status.ToString()
        };
        await _eventPublisher.PublishToUserAsync(userId, eventId, updateTask, cancellationToken);
    }

    private async Task CompleteTaskAsync(
        string userId,
        WorkflowTaskExecutionEntity entity,
        WorkflowTaskExecutionStatus status,
        CancellationToken cancellationToken)
    {
        entity.EndTime = _systemClock.UtcNow;
        await UpdateTaskStatusAsync(userId, entity, status, cancellationToken);
    }

    private static LogScopeContext CreateLogScope(Guid taskId, string taskName) => new()
    {
        { "WorkflowExecutionTaskId", taskId },
        { "WorkflowExecutionTaskName", taskName }
    };

    private CancellationTokenSource CreateTimeoutToken(int? timeoutMs, CancellationToken token)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (timeoutMs.HasValue)
            linkedCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs.Value));
        return linkedCts;
    }

    private async Task ParseWorkflowTaskPlaceholders(WorkflowTask task, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (task == null)
            return;

        await ReplaceTopLevelStrings(task, expressionParser, cancellationToken);
        await ProcessTaskParameters(task, expressionParser, cancellationToken);
        await ProcessManualApproval(task.ManualApproval, expressionParser, cancellationToken);
        await ReplaceStringListItems(task.Dependencies, expressionParser, cancellationToken);
        await ReplaceStringListItems(task.RunOnFailureOf, expressionParser, cancellationToken);
        await ProcessErrorHandling(task.ErrorHandling, expressionParser, cancellationToken);
        await ProcessConditionalProperties(task, expressionParser, cancellationToken);
        await ProcessAgentConfiguration(task.Agent, expressionParser, cancellationToken);

        if (task.Position is not null)
        {
            task.Position.X = await ReplaceDoubleIfPlaceholder(task.Position.X, expressionParser, cancellationToken);
            task.Position.Y = await ReplaceDoubleIfPlaceholder(task.Position.Y, expressionParser, cancellationToken);
        }
    }

    /// <summary>
    /// Applies placeholder replacements to agent configuration.
    /// </summary>
    private async Task ProcessAgentConfiguration(AgentConfiguration? agent, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (agent == null)
            return;

        agent.Instructions = await ReplaceIfNotNull(agent.Instructions, expressionParser, cancellationToken);
        agent.Mode = await ReplaceIfNotNull(agent.Mode, expressionParser, cancellationToken) ?? "execute";

        if (agent.Context is { Count: > 0 })
        {
            foreach (var key in agent.Context.Keys.ToList())
            {
                agent.Context[key] = await ProcessValueDeep(agent.Context[key], expressionParser, cancellationToken) ?? new object();
            }
        }
    }

    private async Task ReplaceTopLevelStrings(WorkflowTask task, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        task.Name = await ReplaceIfNotNull(task.Name, expressionParser, cancellationToken);
        task.Description = await ReplaceIfNotNull(task.Description, expressionParser, cancellationToken);
        task.Output = await ReplaceIfNotNull(task.Output, expressionParser, cancellationToken);
    }

    private async Task ProcessOutputResults(Dictionary<string, object?> outputs, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (outputs is not { Count: > 0 })
            return;

        foreach (var key in outputs.Keys.ToList())
        {
            outputs[key] = await ProcessValueDeep(outputs[key], expressionParser, cancellationToken) ?? null;
        }
    }

    private async Task ProcessTaskParameters(WorkflowTask task, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (task.Parameters is not { Count: > 0 })
            return;

        foreach (var key in task.Parameters.Keys.ToList())
        {
            task.Parameters[key] = await ProcessValueDeep(task.Parameters[key], expressionParser, cancellationToken);
        }

        await _placeholderReplacer.ReplacePlaceholdersInParameters(task.Parameters, expressionParser, cancellationToken);
    }

    private async Task<object?> ProcessValueDeep(object? value, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (value == null)
            return null;

        switch (value)
        {
            case string s:
                return await ReplaceIfNotNull(s, expressionParser, cancellationToken);

            case JObject jObj:
                var pc = TryDeserializePluginContext(jObj);
                if (pc != null)
                {
                    await ApplyPlaceholdersToPluginContext(pc, expressionParser, cancellationToken);
                    return pc;
                }

                foreach (var prop in jObj.Properties().ToList())
                    jObj[prop.Name] = JToken.FromObject(await ProcessValueDeep(jObj[prop.Name], expressionParser, cancellationToken) ?? JValue.CreateNull());
                return jObj;

            case JArray jArray:
                var list = new List<object?>();
                foreach (var item in jArray)
                    list.Add(await ProcessValueDeep(item, expressionParser, cancellationToken));
                return list;

            case IDictionary<string, object?> dict:
                var newDict = new Dictionary<string, object?>();
                foreach (var kvp in dict)
                    newDict[kvp.Key] = await ProcessValueDeep(kvp.Value, expressionParser, cancellationToken);
                return newDict;

            case IEnumerable<object?> enumerable:
            {
                var resultList = new List<object?>();
                foreach (var e in enumerable)
                {
                    resultList.Add(await ProcessValueDeep(e, expressionParser, cancellationToken));
                }
                return resultList;
            }

            case PluginContext pluginCtx:
                await ApplyPlaceholdersToPluginContext(pluginCtx, expressionParser, cancellationToken);
                return pluginCtx;

            default:
                return value;
        }
    }

    private async Task ProcessErrorHandling(ErrorHandling? errorHandling, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (errorHandling == null)
            return;

        if (errorHandling.RetryPolicy != null)
        {
            var rp = errorHandling.RetryPolicy;
            rp.InitialDelayMilliseconds = await ReplaceNumberIfPlaceholder(rp.InitialDelayMilliseconds, expressionParser, cancellationToken);
            rp.MaxDelayMilliseconds = await ReplaceNumberIfPlaceholder(rp.MaxDelayMilliseconds, expressionParser, cancellationToken);
            rp.BackoffCoefficient = await ReplaceDoubleIfPlaceholder(rp.BackoffCoefficient, expressionParser, cancellationToken);
            rp.MaxRetries = await ReplaceNumberIfPlaceholder(rp.MaxRetries, expressionParser, cancellationToken);
        }

        if (errorHandling.TriggerPolicy != null)
        {
            errorHandling.TriggerPolicy.TaskName = await ReplaceIfNotNull(errorHandling.TriggerPolicy.TaskName, expressionParser, cancellationToken);
        }
    }

    private async Task ProcessConditionalProperties(WorkflowTask task, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (task.ExecutionCondition != null)
        {
            task.ExecutionCondition.Expression = await ReplaceIfNotNull(task.ExecutionCondition.Expression, expressionParser, cancellationToken);
            task.ExecutionCondition.Description = await ReplaceIfNotNull(task.ExecutionCondition.Description, expressionParser, cancellationToken);
        }

        if (task.ConditionalBranches is { Count: > 0 })
        {
            foreach (var branch in task.ConditionalBranches)
            {
                branch.Expression = await ReplaceIfNotNull(branch.Expression, expressionParser, cancellationToken);
                branch.TargetTaskName = await ReplaceIfNotNull(branch.TargetTaskName, expressionParser, cancellationToken);
                branch.Description = await ReplaceIfNotNull(branch.Description, expressionParser, cancellationToken);
            }
        }
    }

    private async Task<int> ReplaceNumberIfPlaceholder(int value, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        var str = value.ToString();
        var replaced = await _placeholderReplacer.ReplacePlaceholders(str, expressionParser, cancellationToken);
        if (int.TryParse(replaced, out var num))
            return num;
        return value;
    }

    private async Task<double> ReplaceDoubleIfPlaceholder(double value, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        var str = value.ToString();
        var replaced = await _placeholderReplacer.ReplacePlaceholders(str, expressionParser, cancellationToken);
        if (double.TryParse(replaced, out var num))
            return num;
        return value;
    }

    private async Task ProcessManualApproval(ManualApproval? manualApproval, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (manualApproval == null)
            return;

        manualApproval.Comment = await ReplaceIfNotNull(manualApproval.Comment, expressionParser, cancellationToken);
    }

    private async Task ReplaceStringListItems(List<string>? values, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (values is not { Count: > 0 })
            return;

        for (var index = 0; index < values.Count; index++)
        {
            values[index] = await ReplaceIfNotNull(values[index], expressionParser, cancellationToken);
        }
    }

    private static PluginContext? TryDeserializePluginContext(JObject jObject)
    {
        try { return jObject.ToObject<PluginContext>(); }
        catch { return null; }
    }

    private static List<PluginContext>? TryDeserializePluginContextList(JArray jArray)
    {
        try { return jArray.ToObject<List<PluginContext>>(); }
        catch { return null; }
    }

    private async Task ApplyPlaceholdersToPluginContext(PluginContext context, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        if (context == null)
            return;

        context.Id = await ReplaceIfNotNull(context.Id, expressionParser, cancellationToken);
        context.SourceType = await ReplaceIfNotNull(context.SourceType, expressionParser, cancellationToken);
        context.Format = await ReplaceIfNotNull(context.Format, expressionParser, cancellationToken);
        context.Content = await ReplaceIfNotNull(context.Content, expressionParser, cancellationToken);

        if (context.Metadata.Count > 0)
        {
            await _placeholderReplacer.ReplacePlaceholdersInParameters(context.Metadata, expressionParser, cancellationToken);
        }

        if (context.StructuredData is { Count: > 0 })
        {
            foreach (var dict in context.StructuredData)
            {
                await _placeholderReplacer.ReplacePlaceholdersInParameters(dict, expressionParser, cancellationToken);
            }
        }
    }

    private async Task<string?> ReplaceIfNotNull(string? value, IExpressionParser expressionParser, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : await _placeholderReplacer.ReplacePlaceholders(value, expressionParser, cancellationToken);
    }
}