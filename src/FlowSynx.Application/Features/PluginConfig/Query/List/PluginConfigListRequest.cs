using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Features.PluginConfig.Query.List;

public class PluginConfigListRequest : IRequest<Result<IEnumerable<PluginConfigListResponse>>>
{

}