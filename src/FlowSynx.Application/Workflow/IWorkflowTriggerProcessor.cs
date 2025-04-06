namespace FlowSynx.Application.Workflow;

public interface IWorkflowTriggerProcessor
{
    Task ProcessTriggersAsync(CancellationToken cancellationToken);
}