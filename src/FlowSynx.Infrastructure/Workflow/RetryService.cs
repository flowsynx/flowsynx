using FlowSynx.Application.Features.Workflows.Command.Execute;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly Random _random = new();

    public RetryService(ILogger<RetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, RetryPolicy policy)
    {
        for (int attempt = 1; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < policy.MaxRetries)
            {
                _logger.LogWarning($"Attempt {attempt} failed: {ex.Message}");

                int delay = CalculateDelay(policy, attempt);
                _logger.LogInformation($"Waiting {delay}ms before retry...");
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation failed after {MaxRetries} attempts.", policy.MaxRetries);
                throw;
            }
        }

        throw new InvalidOperationException("Retry mechanism failed unexpectedly.");
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