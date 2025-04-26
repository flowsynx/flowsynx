using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Extensions;
using FlowSynx.Infrastructure.PluginHost;
using FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowTaskExecutor : IWorkflowTaskExecutor
{
    private readonly IPluginTypeService _pluginTypeService;
    private readonly IPlaceholderReplacer _placeholderReplacer;
    private readonly IErrorHandlingStrategyFactory _errorHandlingStrategyFactory;

    public WorkflowTaskExecutor(
        IPluginTypeService pluginTypeService,
        IPlaceholderReplacer placeholderReplacer,
        IErrorHandlingStrategyFactory errorHandlingStrategyFactory)
    {
        _pluginTypeService = pluginTypeService;
        _placeholderReplacer = placeholderReplacer;
        _errorHandlingStrategyFactory = errorHandlingStrategyFactory;
    }

    public async Task<object?> ExecuteAsync(
        string userId, 
        WorkflowTask task, 
        IExpressionParser parser,
        CancellationToken cancellationToken)
    {
        var errorHandlingContext = new ErrorHandlingContext
        {
            TaskName = task.Name,
            RetryCount = 0
        };

        return await ExecuteTaskAsync(userId, task, parser, errorHandlingContext, cancellationToken).ConfigureAwait(false);
    }

    public async Task<object?> ExecuteTaskAsync(
        string userId,
        WorkflowTask task,
        IExpressionParser parser,
        ErrorHandlingContext errorHandlingContext,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CreateTimeoutToken(task.Timeout, cancellationToken);
        var token = timeoutCts.Token;

        var plugin = await _pluginTypeService.Get(userId, task.Type, cancellationToken).ConfigureAwait(false);
        var pluginParameters = PreparePluginParameters(task.Parameters, parser);

        var retryStrategy = _errorHandlingStrategyFactory.Create(task.ErrorHandling);

        try
        {
            token.ThrowIfCancellationRequested();
            return await plugin.ExecuteAsync(pluginParameters, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionTimeout,
                string.Format(Resources.RetryService_TaskTimeoutOnAttempt, task.Name, 0));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new FlowSynxException((int)ErrorCode.WorkflowExecutionTimeout, Resources.RetryService_WorkflowTimeout);
        }
        catch (Exception ex)
        {
            if (token.IsCancellationRequested)
                throw new TimeoutException($"Task '{task.Name}' timed out.");

            var result = await retryStrategy.HandleAsync(errorHandlingContext, token);

            return result switch
            {
                { ShouldRetry: true } => await ExecuteTaskAsync(userId, task, parser, errorHandlingContext, cancellationToken),
                { ShouldSkip: true } => null,
                { ShouldAbortWorkflow: true } => throw new Exception(ex.Message),
                _ => throw new Exception(ex.Message)
            };
        }
    }

    private CancellationTokenSource CreateTimeoutToken(
        int? timeoutMs, 
        CancellationToken cancellationToken)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutMs.HasValue)
            linkedCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs.Value));
        return linkedCts;
    }

    private PluginParameters PreparePluginParameters(
        Dictionary<string, object?>? parameters, 
        IExpressionParser parser)
    {
        var resolvedParameters = parameters ?? new Dictionary<string, object?>();
        _placeholderReplacer.ReplacePlaceholdersInParameters(resolvedParameters, parser);
        return resolvedParameters.ToPluginParameters();
    }
}
