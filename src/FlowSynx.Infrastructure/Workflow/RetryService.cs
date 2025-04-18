using FlowSynx.Application.Features.Workflows.Command.Execute;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;

    public RetryService(ILogger<RetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, RetryPolicy policy)
    {
        int attempt = 0;

        while (attempt < policy.MaxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogWarning($"Attempt {attempt} failed: {ex.Message}");

                if (attempt >= policy.MaxRetries)
                {
                    _logger.LogError($"Operation failed after {policy.MaxRetries} attempts.");
                    throw new Exception(ex.Message);
                }

                int delay = CalculateDelay(policy, attempt);
                _logger.LogInformation($"Retrying in {delay}ms...");
                await Task.Delay(delay);
            }
        }

        throw new Exception("Retry mechanism failed unexpectedly.");
    }

    private int CalculateDelay(RetryPolicy policy, int attempt)
    {
        int delay = policy.InitialDelay;

        switch (policy.BackoffStrategy)
        {
            case BackoffStrategy.Exponential:
                delay = policy.InitialDelay * (int)Math.Pow(2, attempt - 1);
                break;
            case BackoffStrategy.Linear:
                delay = policy.InitialDelay * attempt;
                break;
            case BackoffStrategy.Jitter:
                var rand = new Random();
                delay = rand.Next(policy.InitialDelay, policy.MaxDelay);
                break;
            case BackoffStrategy.Fixed:
            default:
                delay = policy.InitialDelay;
                break;
        }

        return Math.Min(delay, policy.MaxDelay);
    }
}