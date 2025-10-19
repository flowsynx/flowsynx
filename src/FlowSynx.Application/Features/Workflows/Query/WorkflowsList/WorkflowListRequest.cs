using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowsList;

public class WorkflowListRequest : IRequest<PaginatedResult<WorkflowListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
