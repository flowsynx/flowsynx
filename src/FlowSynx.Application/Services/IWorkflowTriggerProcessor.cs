namespace FlowSynx.Application.Services;

public interface IWorkflowTriggerProcessor
{
    Task ProcessTriggersAsync(CancellationToken cancellationToken);
}