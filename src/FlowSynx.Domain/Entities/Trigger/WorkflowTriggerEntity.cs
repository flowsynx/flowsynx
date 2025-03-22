using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Entities.Trigger;

public class WorkflowTriggerEntity: AuditableEntity<Guid>
{
    public required Guid WorkflowId { get; set; }
    public required string UserId { get; set; }
    public WorkflowTriggerType Type { get; set; } = WorkflowTriggerType.Manual;
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    public WorkflowEntity Workflow { get; set; }
}