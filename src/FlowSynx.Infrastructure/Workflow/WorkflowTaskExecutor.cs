﻿using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
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

    public WorkflowTaskExecutor(
        ILogger<WorkflowTaskExecutor> logger,
        IPluginTypeService pluginTypeService,
        IPlaceholderReplacer placeholderReplacer,
        IErrorHandlingStrategyFactory errorHandlingStrategyFactory,
        IWorkflowTaskExecutionService workflowTaskExecutionService,
        ISystemClock systemClock,
        ILocalization localization)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pluginTypeService = pluginTypeService ?? throw new ArgumentNullException(nameof(pluginTypeService));
        _placeholderReplacer = placeholderReplacer ?? throw new ArgumentNullException(nameof(placeholderReplacer));
        _errorHandlingStrategyFactory = errorHandlingStrategyFactory ?? throw new ArgumentNullException(nameof(errorHandlingStrategyFactory));
        _workflowTaskExecutionService = workflowTaskExecutionService ?? throw new ArgumentNullException(nameof(workflowTaskExecutionService));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task<object?> ExecuteAsync(
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
            await UpdateTaskStatusAsync(taskExecution, WorkflowTaskExecutionStatus.Running, globalCancellationToken);
            _logger.LogInformation("Workflow task '{TaskName}' started.", task.Name);

            var context = new ErrorHandlingContext { TaskName = task.Name, RetryCount = 0 };
            return await ExecuteTaskAsync(executionContext.UserId, task, taskExecution, parser, context, 
                globalCancellationToken, taskCancellationToken);
        }
    }

    public async Task<object?> ExecuteTaskAsync(
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

        var plugin = await _pluginTypeService.Get(userId, task.Type, token);
        var pluginParameters = PreparePluginParameters(task.Parameters, parser);
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
            
            await CompleteTaskAsync(taskExecution, WorkflowTaskExecutionStatus.Completed, globalCancellationToken);
            _logger.LogInformation("Workflow task '{TaskName}' completed.", task.Name);
            return output;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            await CompleteTaskAsync(taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
            _logger.LogError($"Workflow task '{task.Name}' canceled.");
            throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionCanceled,
                _localization.Get("RetryService_TaskCanceled", task.Name, 0));
        }
        catch (OperationCanceledException) when (globalCancellationToken.IsCancellationRequested)
        {
            await CompleteTaskAsync(taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
            _logger.LogError($"Workflow task '{task.Name}' canceled.");
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionCanceled,
                _localization.Get("RetryService_WorkflowCanceled", task.Name, 0));
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested)
            {
                await CompleteTaskAsync(taskExecution, WorkflowTaskExecutionStatus.Canceled, globalCancellationToken);
                _logger.LogError($"Workflow task '{task.Name}' canceled.");
                throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionCanceled,
                    _localization.Get("RetryService_TaskCanceled", task.Name, 0));
            }

            var result = await retryStrategy.HandleAsync(errorContext, token);
            if (result?.ShouldRetry == true)
            {
                await UpdateTaskStatusAsync(taskExecution, WorkflowTaskExecutionStatus.Retrying, globalCancellationToken);
                _logger.LogWarning("Workflow task '{TaskName}' retrying.", task.Name);
                return await ExecuteTaskAsync(userId, task, taskExecution, parser, errorContext, globalCancellationToken, token);
            }

            if (result?.ShouldSkip == true)
            {
                await CompleteTaskAsync(taskExecution, WorkflowTaskExecutionStatus.Completed, globalCancellationToken);
                _logger.LogWarning("Workflow task '{TaskName}' skipped.", task.Name);
                return null;
            }

            await FailTaskAsync(taskExecution, ex, globalCancellationToken, task.Name);
        }

        return null; // unreachable, all branches throw
    }

    private async Task FailTaskAsync(
        WorkflowTaskExecutionEntity entity,
        Exception ex,
        CancellationToken cancellationToken,
        string taskName)
    {
        await CompleteTaskAsync(entity, WorkflowTaskExecutionStatus.Failed, cancellationToken);
        _logger.LogError(ex, "Workflow task '{TaskName}' failed: {Message}", taskName, ex.Message);
        throw new Exception(ex.Message, ex);
    }

    private async Task UpdateTaskStatusAsync(
        WorkflowTaskExecutionEntity entity,
        WorkflowTaskExecutionStatus status,
        CancellationToken cancellationToken)
    {
        entity.Status = status;
        await _workflowTaskExecutionService.Update(entity, cancellationToken);
    }

    private async Task CompleteTaskAsync(
        WorkflowTaskExecutionEntity entity,
        WorkflowTaskExecutionStatus status,
        CancellationToken cancellationToken)
    {
        entity.EndTime = _systemClock.UtcNow;
        await UpdateTaskStatusAsync(entity, status, cancellationToken);
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

    private PluginParameters PreparePluginParameters(Dictionary<string, object?>? parameters, IExpressionParser parser)
    {
        var resolved = parameters ?? new Dictionary<string, object?>();
        _placeholderReplacer.ReplacePlaceholdersInParameters(resolved, parser);
        return resolved.ToPluginParameters();
    }
}