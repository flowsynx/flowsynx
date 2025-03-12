using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.Version.Query;

public class VersionRequest : IRequest<Result<VersionResponse>>
{

}