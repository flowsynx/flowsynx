using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailsList;

public class AuditTrailsListRequest : IAction<PaginatedResult<AuditTrailsListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
