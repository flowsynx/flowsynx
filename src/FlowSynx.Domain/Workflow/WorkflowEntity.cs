using FlowSynx.Domain.Trigger;

namespace FlowSynx.Domain.Workflow;

public class WorkflowEntity : AuditableEntity<Guid>, ISoftDeletable
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Definition { get; set; }
    public string? SchemaUrl { get; set; }
    public bool IsDeleted { get; set; } = false;

    public List<WorkflowExecutionEntity> Executions { get; set; } = new();
    public List<WorkflowTriggerEntity> Triggers { get; set; } = new();
}
