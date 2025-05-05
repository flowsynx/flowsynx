using FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow.BackoffStrategies;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class ErrorHandlingStrategyFactory: IErrorHandlingStrategyFactory
{
    private readonly ILogger<ErrorHandlingStrategyFactory> _logger;

    public ErrorHandlingStrategyFactory(ILogger<ErrorHandlingStrategyFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public IErrorHandlingStrategy Create(ErrorHandling? errorHandling)
    {
        return errorHandling?.Strategy switch
        {
            ErrorStrategy.Retry => CreateRetry(errorHandling.RetryPolicy),
            ErrorStrategy.Skip => new SkipStrategy(_logger),
            ErrorStrategy.Abort => new AbortStrategy(_logger),
            _ => throw new ArgumentException($"Unknown error handling strategy: {errorHandling?.Strategy}")
        };
    }

    private IErrorHandlingStrategy CreateRetry(RetryPolicy? retryPolicy)
    {
        IBackoffStrategy backoff = retryPolicy?.BackoffStrategy switch
        {
            BackoffStrategy.Exponential => new ExponentialBackoffStrategy(
                retryPolicy.InitialDelay,
                retryPolicy.BackoffCoefficient
            ),
            BackoffStrategy.Linear => new LinearBackoffStrategy(
                retryPolicy.InitialDelay
            ),
            BackoffStrategy.Jitter => new JitterBackoffStrategy(
                retryPolicy.InitialDelay,
                retryPolicy.BackoffCoefficient
            ),
            BackoffStrategy.Fixed => new FixedBackoffStrategy(
                retryPolicy.InitialDelay
            ),
            _ => throw new ArgumentException($"Unknown backoff strategy type: {retryPolicy?.BackoffStrategy}")
        };

        return new RetryStrategy(retryPolicy.MaxRetries, backoff, _logger);
    }
}
