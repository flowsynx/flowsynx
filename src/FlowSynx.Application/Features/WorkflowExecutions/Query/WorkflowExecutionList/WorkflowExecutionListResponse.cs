using FlowSynx.Domain.Log;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

public class WorkflowExecutionListResponse
{
    public Guid Id { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public DateTime ExecutionStart { get; set; }
    public DateTime? ExecutionEnd { get; set; }
}