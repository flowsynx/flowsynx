namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class JitterBackoffStrategy(int initialDelay, double backoffCoefficient = 0.5) : IBackoffStrategy
{
    private readonly Random _random = new();

    public TimeSpan GetDelay(int retryCount)
    {
        var baseMs = initialDelay * Math.Pow(2, retryCount); // Exponential growth
        var jitter = _random.NextDouble() * backoffCoefficient * baseMs;
        return TimeSpan.FromMilliseconds(baseMs + jitter);
    }
}
