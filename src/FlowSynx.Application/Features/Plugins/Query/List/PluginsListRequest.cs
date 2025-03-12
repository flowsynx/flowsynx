using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.Plugins.Query.List;

public class PluginsListRequest : IRequest<Result<IEnumerable<PluginsListResponse>>>
{

}