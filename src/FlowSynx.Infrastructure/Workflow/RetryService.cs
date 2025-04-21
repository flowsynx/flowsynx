using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly IWorkflowTaskExecutor _taskExecutor;
    private readonly Random _random = new();

    public RetryService(ILogger<RetryService> logger, 
        IWorkflowTaskExecutor taskExecutor)
    {
        _logger = logger;
        _taskExecutor = taskExecutor;
    }

    public async Task<object?> ExecuteAsync(
        string userId,
        WorkflowTask task,
        IExpressionParser parser,
        CancellationToken cancellationToken)
    {
        if (task.RetryPolicy == null)
            task.RetryPolicy = new RetryPolicy { MaxRetries = 1};

        for (int attempt = 1; attempt <= task.RetryPolicy.MaxRetries; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (task.Timeout.HasValue)
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(task.Timeout.Value));

            try
            {
                return await _taskExecutor.ExecuteAsync(userId, task, parser, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new FlowSynxException((int)ErrorCode.WorkflowTaskExecutionTimeout, $"Task '{task.Name}' timeout on attempt {attempt}.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new FlowSynxException((int)ErrorCode.WorkflowExecutionTimeout, $"Workflow execution timeout");
            }
            catch (Exception ex) when (attempt < task.RetryPolicy.MaxRetries)
            {
                _logger.LogWarning(string.Format(Resources.RetryService_AttemptFailed, attempt, ex.Message));

                int delay = CalculateDelay(task.RetryPolicy, attempt);
                _logger.LogInformation(string.Format(Resources.RetryService_WaitingBeforeRetry, delay));
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format(Resources.RetryService_OperationFailedAfterAttempts, task.RetryPolicy.MaxRetries));
                throw;
            }
        }

        throw new InvalidOperationException(Resources.RetryService_RetryFailedUnexpectedly);
    }

    private int CalculateDelay(RetryPolicy policy, int attempt)
    {
        return policy.BackoffStrategy switch
        {
            BackoffStrategy.Exponential => Math.Min(policy.InitialDelay * (int)Math.Pow(2, attempt - 1), policy.MaxDelay),
            BackoffStrategy.Linear => Math.Min(policy.InitialDelay * attempt, policy.MaxDelay),
            BackoffStrategy.Jitter => _random.Next(policy.InitialDelay, policy.MaxDelay),
            _ => policy.InitialDelay
        };
    }
}