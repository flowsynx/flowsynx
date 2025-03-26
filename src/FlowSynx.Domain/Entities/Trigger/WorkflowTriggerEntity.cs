using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Entities.Trigger;

public class WorkflowTriggerEntity: AuditableEntity<Guid>, ISoftDeletable
{
    public required Guid WorkflowId { get; set; }
    public required string UserId { get; set; }
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public WorkflowTriggerStatus Status { get; set; } = WorkflowTriggerStatus.Active;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public bool IsDeleted { get; set; } = false;

    public WorkflowEntity Workflow { get; set; }
}