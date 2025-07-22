using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionApprovals;

public class WorkflowExecutionApprovalsRequest : IRequest<Result<IEnumerable<WorkflowExecutionApprovalsResponse>>>
{
    public required string WorkflowId { get; set; }
    public required string WorkflowExecutionId { get; set; }
}