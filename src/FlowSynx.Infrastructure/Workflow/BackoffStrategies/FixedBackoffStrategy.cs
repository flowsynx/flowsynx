namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class FixedBackoffStrategy : IBackoffStrategy
{
    private readonly int _delay;

    public FixedBackoffStrategy(int delay) => _delay = delay;

    public TimeSpan GetDelay(int retryCount) => TimeSpan.FromMilliseconds(_delay);
}