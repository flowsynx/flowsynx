namespace FlowSynx.Infrastructure.Workflow;

public class SemaphoreFactory : ISemaphoreFactory
{
    public SemaphoreSlim Create(int initialCount) => new(initialCount);
}