using FlowSynx.Domain.Primitives;
using MediatR;

namespace FlowSynx.Application.Features.AuditTrails.Query.AuditTrailsList;

public class AuditTrailsListRequest : IRequest<PaginatedResult<AuditTrailsListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
