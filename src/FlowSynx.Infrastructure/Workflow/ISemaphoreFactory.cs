namespace FlowSynx.Infrastructure.Workflow;

public interface ISemaphoreFactory
{
    SemaphoreSlim Create(int initialCount);
}