namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public interface IBackoffStrategy
{
    TimeSpan GetDelay(int retryCount);
}