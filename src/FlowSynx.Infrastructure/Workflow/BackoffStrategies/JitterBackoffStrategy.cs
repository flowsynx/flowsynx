namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class JitterBackoffStrategy : IBackoffStrategy
{
    private readonly int _initialDelay;
    private readonly double _factor;
    private readonly Random _random;

    public JitterBackoffStrategy(int initialDelay, double factor = 0.5)
    {
        _initialDelay = initialDelay;
        _factor = factor;
        _random = new Random();
    }

    public TimeSpan GetDelay(int retryCount)
    {
        var baseMs = _initialDelay * Math.Pow(2, retryCount); // Exponential growth
        var jitter = _random.NextDouble() * _factor * baseMs;
        return TimeSpan.FromMilliseconds(baseMs + jitter);
    }
}
