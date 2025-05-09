namespace FlowSynx.Domain.Trigger;

public interface IWorkflowTriggerService
{
    Task<IReadOnlyCollection<WorkflowTriggerEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowTriggerEntity>> GetByWorkflowIdAsync(Guid workflowId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WorkflowTriggerEntity>> GetActiveTriggersByTypeAsync(WorkflowTriggerType type, 
        CancellationToken cancellationToken);

    Task<WorkflowTriggerEntity?> GetByIdAsync(Guid workflowId, Guid triggerId, 
        CancellationToken cancellationToken);

    Task AddAsync(WorkflowTriggerEntity triggerEntity, 
        CancellationToken cancellationToken);

    Task UpdateAsync(WorkflowTriggerEntity triggerEntity, 
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(WorkflowTriggerEntity triggerEntity, 
        CancellationToken cancellationToken);
}