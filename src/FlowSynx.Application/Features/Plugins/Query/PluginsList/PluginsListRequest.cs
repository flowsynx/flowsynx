using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Query.PluginsList;

public class PluginsListRequest : IRequest<Result<IEnumerable<PluginsListResponse>>>
{

}