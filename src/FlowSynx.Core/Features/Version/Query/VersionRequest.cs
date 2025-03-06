using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.Version.Query;

public class VersionRequest : IRequest<Result<VersionResponse>>
{

}