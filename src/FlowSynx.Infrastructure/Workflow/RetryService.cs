using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;

    public RetryService(ILogger<RetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int maxRetries, TimeSpan delay)
    {
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogWarning($"Attempt {attempt} failed: {ex.Message}");

                if (attempt >= maxRetries)
                {
                    _logger.LogError($"Operation failed after {maxRetries} attempts.");
                    throw new Exception(ex.Message);
                }

                await Task.Delay(delay);
            }
        }

        throw new Exception("Retry mechanism failed unexpectedly.");
    }
}