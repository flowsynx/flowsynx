using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Audit.Query.List;

public class AuditsListRequest : IRequest<Result<IEnumerable<AuditsListResponse>>>
{

}