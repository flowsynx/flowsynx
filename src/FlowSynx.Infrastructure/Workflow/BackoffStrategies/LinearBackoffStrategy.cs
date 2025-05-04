namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class LinearBackoffStrategy(int initialDelay) : IBackoffStrategy
{
    public TimeSpan GetDelay(int retryCount) 
    {
        return TimeSpan.FromMilliseconds(initialDelay * retryCount);
    }
}