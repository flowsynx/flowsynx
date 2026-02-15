using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.WorkflowExecutions;

public class WorkflowExecutionArtifact : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid WorkflowExecutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object Content { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkflowExecution? WorkflowExecution { get; set; }
}