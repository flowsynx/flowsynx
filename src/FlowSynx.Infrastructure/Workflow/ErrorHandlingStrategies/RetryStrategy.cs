using FlowSynx.Application.Localizations;
using FlowSynx.Infrastructure.Workflow.BackoffStrategies;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class RetryStrategy(int maxRetries, IBackoffStrategy backoffStrategy, ILogger logger) : IErrorHandlingStrategy
{
    public async Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context,
        CancellationToken cancellationToken)
    {
        if (context.RetryCount < maxRetries)
        {
            var delay = backoffStrategy.GetDelay(context.RetryCount);
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

        logger.LogError(Localization.Get("RetryService_OperationFailedAfterAttempts", context.TaskName, maxRetries));
        return new ErrorHandlingResult { ShouldAbortWorkflow = true };
    }
}
