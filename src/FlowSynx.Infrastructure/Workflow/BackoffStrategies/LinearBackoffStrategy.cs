namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class LinearBackoffStrategy : IBackoffStrategy
{
    private readonly int _initialDelay;

    public LinearBackoffStrategy(int initialDelay)
    {
        _initialDelay = initialDelay;
    }

    public TimeSpan GetDelay(int retryCount) 
    {
        return TimeSpan.FromMilliseconds(_initialDelay * retryCount);
    }
}