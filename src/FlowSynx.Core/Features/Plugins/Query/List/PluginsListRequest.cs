using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.Plugins.Query.List;

public class PluginsListRequest : IRequest<Result<IEnumerable<PluginsListResponse>>>
{

}