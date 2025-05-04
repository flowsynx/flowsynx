namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class FixedBackoffStrategy(int delay) : IBackoffStrategy
{
    public TimeSpan GetDelay(int retryCount) => TimeSpan.FromMilliseconds(delay);
}