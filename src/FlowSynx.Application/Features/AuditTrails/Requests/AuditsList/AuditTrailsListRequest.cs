using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailsList;

public class AuditTrailsListRequest : IAction<PaginatedResult<AuditTrailsListResult>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
