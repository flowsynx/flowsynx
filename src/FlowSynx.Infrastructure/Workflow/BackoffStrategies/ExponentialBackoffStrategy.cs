namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class ExponentialBackoffStrategy(int initialDelay, double backoffCoefficient = 2.0) : IBackoffStrategy
{
    public TimeSpan GetDelay(int retryCount)
    {
        return TimeSpan.FromMilliseconds(initialDelay * Math.Pow(backoffCoefficient, retryCount));
    }
}