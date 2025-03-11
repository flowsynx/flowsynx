namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowEntity: AuditableEntity<Guid>
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Definition { get; set; }

    public List<WorkflowExecutionEntity> Executions { get; set; } = new();
}