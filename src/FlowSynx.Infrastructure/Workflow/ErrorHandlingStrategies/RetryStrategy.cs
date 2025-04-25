using FlowSynx.Infrastructure.Workflow.BackoffStrategies;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class RetryStrategy : IErrorHandlingStrategy
{
    private readonly int _maxRetries;
    private readonly IBackoffStrategy _backoffStrategy;
    private readonly ILogger _logger;

    public RetryStrategy(int maxRetries, IBackoffStrategy backoffStrategy, ILogger logger)
    {
        _maxRetries = maxRetries;
        _backoffStrategy = backoffStrategy;
        _logger = logger;
    }

    public async Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context,
        CancellationToken cancellationToken)
    {
        if (context.RetryCount < _maxRetries)
        {
            var delay = _backoffStrategy.GetDelay(context.RetryCount);
            context.RetryCount++;

            try
            {
                await Task.Delay(delay, cancellationToken);
                return new ErrorHandlingResult { ShouldRetry = true };
            }
            catch (TaskCanceledException)
            {
                return new ErrorHandlingResult { ShouldAbortWorkflow = true };
            }
        }
        _logger.LogError(string.Format(Resources.RetryService_OperationFailedAfterAttempts, _maxRetries));
        return new ErrorHandlingResult { ShouldAbortWorkflow = true };
    }
}
