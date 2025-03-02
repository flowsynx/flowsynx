namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowDefination: AuditableEntity<Guid>
{
    public required string Name { get; set; }
    public required string Template { get; set; }
}