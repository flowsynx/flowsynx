using FlowSynx.Domain.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Audit.Query.AuditsList;

public class AuditsListRequest : IRequest<PaginatedResult<AuditsListResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
