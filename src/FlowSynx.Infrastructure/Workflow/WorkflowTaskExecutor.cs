using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.Logging;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.Parsers;
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

    public WorkflowTaskExecutor(
        ILogger<WorkflowTaskExecutor> logger,
        IPluginTypeService pluginTypeService,
        IPlaceholderReplacer placeholderReplacer,
        IErrorHandlingStrategyFactory errorHandlingStrategyFactory,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ISystemClock systemClock,
        ILocalization localization,
        IEventPublisher eventPublisher,
        ITriggeredTaskQueue triggeredTaskQueue)
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
    }

    public async Task<TaskOutput> ExecuteAsync(
        WorkflowExecutionContext executionContext,
        WorkflowTask task,
        IExpressionParser parser,
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
            return await ExecuteTaskAsync(executionContext.UserId, task, taskExecution, parser, context, 
                globalCancellationToken, taskCancellationToken);
        }
    }

    public async Task<TaskOutput> ExecuteTaskAsync(
        string userId,
        WorkflowTask task,
        WorkflowTaskExecutionEntity taskExecution,
        IExpressionParser parser,
        ErrorHandlingContext errorContext,
        CancellationToken globalCancellationToken,
        CancellationToken taskCancellationToken)
    {
        using var timeoutCts = CreateTimeoutToken(task.Timeout, taskCancellationToken);
        var token = timeoutCts.Token;

        ParseWorkflowTaskPlaceholders(task, parser);
        var plugin = await _pluginTypeService.Get(userId, task.Type, token);
        var pluginParameters = task.Parameters.ToPluginParameters();
        var retryStrategy = _errorHandlingStrategyFactory.Create(task.ErrorHandling);

        try
        {
            token.ThrowIfCancellationRequested();
            object? output;
            var result = await plugin.ExecuteAsync(pluginParameters, token);
            if (result is null && !string.IsNullOrEmpty(task.Output)) 
                output = new PluginContext(task.Name, "Data") { Content = task.Output };
            else
                output = result;
            
            await CompleteTaskAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Completed, globalCancellationToken);
            _logger.LogInformation("Workflow task '{TaskName}' completed.", task.Name);
            return TaskOutput.Success(output);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            await CompleteTaskAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
            _logger.LogError($"Workflow task '{task.Name}' canceled.");
            throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionCanceled,
                _localization.Get("RetryService_TaskCanceled", task.Name, 0));
        }
        catch (OperationCanceledException) when (globalCancellationToken.IsCancellationRequested)
        {
            await CompleteTaskAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
            _logger.LogError($"Workflow task '{task.Name}' canceled.");
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionCanceled,
                _localization.Get("RetryService_WorkflowCanceled", task.Name, 0));
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested)
            {
                await CompleteTaskAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
                _logger.LogError($"Workflow task '{task.Name}' canceled.");
                throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionCanceled,
                    _localization.Get("RetryService_TaskCanceled", task.Name, 0));
            }

            var result = await retryStrategy.HandleAsync(errorContext, token);

            if (result?.ShouldTriggerTask == true && !string.IsNullOrWhiteSpace(result.TaskToTrigger))
            {
                _triggeredTaskQueue.Enqueue(taskExecution.WorkflowExecutionId, result.TaskToTrigger);
                _logger.LogInformation("Triggered task '{TaskToTrigger}' due to error in '{TaskName}'.",
                    result.TaskToTrigger, task.Name);
            }

            if (result?.ShouldRetry == true)
            {
                await UpdateTaskStatusAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Retrying, globalCancellationToken);
                _logger.LogWarning("Workflow task '{TaskName}' retrying.", task.Name);
                return await ExecuteTaskAsync(userId, task, taskExecution, parser, errorContext, globalCancellationToken, token);
            }

            if (result?.ShouldSkip == true)
            {
                await CompleteTaskAsync(userId, taskExecution, WorkflowTaskExecutionStatus.Completed, globalCancellationToken);
                _logger.LogWarning("Workflow task '{TaskName}' skipped.", task.Name);
                return TaskOutput.Success(null);
            }

            await FailTaskAsync(userId, taskExecution, ex, globalCancellationToken, task.Name);
        }

        return TaskOutput.Success(null); // unreachable, all branches throw
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

    public void ParseWorkflowTaskPlaceholders(WorkflowTask task, IExpressionParser parser)
    {
        if (task == null)
            return;

        ReplaceTopLevelStrings(task, parser);
        ProcessTaskParameters(task, parser);
        ProcessManualApproval(task.ManualApproval, parser);
        ReplaceStringListItems(task.Dependencies, parser);
    }

    /// <summary>
    /// Replaces placeholders in the top-level string fields of a workflow task.
    /// </summary>
    private void ReplaceTopLevelStrings(WorkflowTask task, IExpressionParser parser)
    {
        task.Name = ReplaceIfNotNull(task.Name, parser);
        task.Description = ReplaceIfNotNull(task.Description, parser);
        task.Output = ReplaceIfNotNull(task.Output, parser);
    }

    /// <summary>
    /// Normalizes task parameter values by applying placeholder replacements and converting JSON payloads.
    /// </summary>
    private void ProcessTaskParameters(WorkflowTask task, IExpressionParser parser)
    {
        if (task.Parameters is not { Count: > 0 })
            return;

        foreach (var key in task.Parameters.Keys.ToList())
        {
            task.Parameters[key] = NormalizeParameterValue(task.Parameters[key], parser);
        }

        _placeholderReplacer.ReplacePlaceholdersInParameters(task.Parameters, parser);
    }

    private object? NormalizeParameterValue(object? value, IExpressionParser parser)
    {
        if (value is null)
            return null;

        return value switch
        {
            JObject jObject => ProcessPluginContextObject(jObject, parser),
            JArray jArray => ProcessPluginContextArray(jArray, parser),
            string s => ReplaceIfNotNull(s, parser),
            _ => value
        };
    }

    private object ProcessPluginContextObject(JObject jObject, IExpressionParser parser)
    {
        var pluginContext = TryDeserializePluginContext(jObject);
        if (pluginContext == null)
            return jObject;

        ApplyPlaceholdersToPluginContext(pluginContext, parser);
        return pluginContext;
    }

    private object ProcessPluginContextArray(JArray jArray, IExpressionParser parser)
    {
        var pluginContexts = TryDeserializePluginContextList(jArray);
        if (pluginContexts == null)
            return jArray;

        foreach (var context in pluginContexts)
        {
            ApplyPlaceholdersToPluginContext(context, parser);
        }

        return pluginContexts;
    }

    /// <summary>
    /// Applies placeholder replacements to manual approval metadata.
    /// </summary>
    private void ProcessManualApproval(ManualApproval? manualApproval, IExpressionParser parser)
    {
        if (manualApproval == null)
            return;

        manualApproval.Instructions = ReplaceIfNotNull(manualApproval.Instructions, parser);
        manualApproval.DefaultAction = ReplaceIfNotNull(manualApproval.DefaultAction, parser);

        ReplaceStringListItems(manualApproval.Approvers, parser);
    }

    /// <summary>
    /// Applies placeholder replacements over a list of strings when items are present.
    /// </summary>
    private void ReplaceStringListItems(List<string>? values, IExpressionParser parser)
    {
        if (values is not { Count: > 0 })
            return;

        for (var index = 0; index < values.Count; index++)
        {
            values[index] = ReplaceIfNotNull(values[index], parser);
        }
    }

    private static PluginContext? TryDeserializePluginContext(JObject jObject)
    {
        try
        {
            return jObject.ToObject<PluginContext>();
        }
        catch
        {
            return null;
        }
    }

    private static List<PluginContext>? TryDeserializePluginContextList(JArray jArray)
    {
        try
        {
            return jArray.ToObject<List<PluginContext>>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Applies placeholder replacements to all string-like properties and internal dictionaries of PluginContext.
    /// </summary>
    private void ApplyPlaceholdersToPluginContext(PluginContext context, IExpressionParser parser)
    {
        if (context == null)
            return;

        // Replace placeholders in all string-based properties
        context.Id = ReplaceIfNotNull(context.Id, parser);
        context.SourceType = ReplaceIfNotNull(context.SourceType, parser);
        context.Format = ReplaceIfNotNull(context.Format, parser);
        context.Content = ReplaceIfNotNull(context.Content, parser);

        // Replace placeholders inside Metadata dictionary
        if (context.Metadata.Count > 0)
        {
            _placeholderReplacer.ReplacePlaceholdersInParameters(context.Metadata, parser);
        }

        // Replace placeholders inside StructuredData (list of dictionaries)
        if (context.StructuredData is { Count: > 0 })
        {
            foreach (var dict in context.StructuredData)
            {
                _placeholderReplacer.ReplacePlaceholdersInParameters(dict, parser);
            }
        }

        // RawData (byte[]) ignored — no placeholders applied
    }

    private string? ReplaceIfNotNull(string? value, IExpressionParser parser)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : _placeholderReplacer.ReplacePlaceholders(value, parser);
    }
}