namespace FlowSynx.Domain.Trigger;

public interface IWorkflowTriggerService
{
    Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WorkflowTriggerEntity>> ActiveTriggers(WorkflowTriggerType type, CancellationToken cancellationToken);
    Task<WorkflowTriggerEntity?> Get(Guid workflowTriggerId, CancellationToken cancellationToken);
    Task Add(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task Update(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task<bool> Delete(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}