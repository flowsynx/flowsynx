using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

public class WorkflowExecutionDetailsResponse
{
    public required Guid WorkflowId { get; set; }
    public required Guid ExecutionId { get; set; }
    public required string Workflow { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }
}