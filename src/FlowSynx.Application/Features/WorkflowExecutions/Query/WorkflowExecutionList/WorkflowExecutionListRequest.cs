using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowExecutionList;

public class WorkflowExecutionListRequest : IRequest<PaginatedResult<WorkflowExecutionListResponse>>
{
    public required string WorkflowId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
