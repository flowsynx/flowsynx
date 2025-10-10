namespace FlowSynx.Domain.Workflow;

public class WorkflowApprovalEntity: AuditableEntity<Guid>
{
    public required string UserId { get; set; }
    public required Guid WorkflowId { get; set; }
    public required Guid ExecutionId { get; set; }
    public required string TaskName { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
    public string? Approver { get; set; }
    public DateTime? DecidedAt { get; set; }
    public WorkflowApprovalStatus Status { get; set; } = WorkflowApprovalStatus.Pending;
    public string? Comments { get; set; }
}