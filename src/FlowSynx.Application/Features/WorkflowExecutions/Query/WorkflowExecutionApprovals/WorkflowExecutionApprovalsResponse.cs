namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;

public class WorkflowExecutionApprovalsResponse
{
    public Guid Id { get; set; }
    public required Guid WorkflowId { get; set; }
    public required Guid ExecutionId { get; set; }
    public required string TaskName { get; set; } = default!;
    public string RequestedBy { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
    public string? Status { get; set; }
    public string? Comments { get; set; }
}