using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Audit.Query.AuditsList;

public class AuditsListRequest : IRequest<Result<IEnumerable<AuditsListResponse>>>
{

}