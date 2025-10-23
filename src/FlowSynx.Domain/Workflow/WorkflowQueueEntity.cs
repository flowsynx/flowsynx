namespace FlowSynx.Domain.Workflow;

public class WorkflowQueueEntity: AuditableEntity<Guid>
{
    public required string UserId { get; set; }
    public required Guid WorkflowId { get; set; }
    public required Guid ExecutionId { get; set; }
    public WorkflowQueueStatus Status { get; set; }
    public string? TriggerPayload { get; set; }
}
