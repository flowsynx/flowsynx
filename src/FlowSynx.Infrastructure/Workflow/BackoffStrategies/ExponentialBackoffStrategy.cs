namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class ExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly int _initialDelay;
    private readonly double _factor;

    public ExponentialBackoffStrategy(int initialDelay, double factor = 2.0)
    {
        _initialDelay = initialDelay;
        _factor = factor;
    }

    public TimeSpan GetDelay(int retryCount)
    {
        return TimeSpan.FromMilliseconds(_initialDelay * Math.Pow(_factor, retryCount));
    }
}