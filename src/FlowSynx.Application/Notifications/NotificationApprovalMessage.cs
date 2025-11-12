namespace FlowSynx.Application.Notifications;

public record NotificationApprovalMessage
{
    public Guid WorkflowId { get; set; }
    public Guid ExecutionId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}