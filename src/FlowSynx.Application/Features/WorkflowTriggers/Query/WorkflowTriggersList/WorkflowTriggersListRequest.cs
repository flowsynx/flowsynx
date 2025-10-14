using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Workflows.Query.WorkflowTriggersList;

public class WorkflowTriggersListRequest : IRequest<PaginatedResult<WorkflowTriggersListResponse>>
{
    public required string WorkflowId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
