using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowTriggerService
{
    Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(WorkflowTriggerType type, CancellationToken cancellationToken);
    Task<WorkflowTriggerEntity?> Get(Guid workflowTriggerId, CancellationToken cancellationToken);
    Task Add(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task Update(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task<bool> Delete(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
}