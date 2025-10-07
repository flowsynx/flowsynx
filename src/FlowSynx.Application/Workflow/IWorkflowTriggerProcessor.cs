namespace FlowSynx.Application.Workflow;

public interface IWorkflowTriggerProcessor
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ProcessTriggersAsync(CancellationToken cancellationToken);
}