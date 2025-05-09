using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

public class WorkflowExecutionListRequest : IRequest<Result<IEnumerable<WorkflowExecutionListResponse>>>
{
    public required string WorkflowId { get; set; }
}