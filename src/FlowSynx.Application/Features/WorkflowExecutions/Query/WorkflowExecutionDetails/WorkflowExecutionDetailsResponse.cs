using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionDetails;

public class WorkflowExecutionDetailsResponse
{
    public Guid Id { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }
}