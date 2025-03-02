using FlowSynx.Core.Wrapper;
using MediatR;

namespace FlowSynx.Core.Features.PluginConfig.Query.List;

public class PluginConfigListRequest : IRequest<Result<IEnumerable<PluginConfigListResponse>>>
{
    public string? UserId { get; set; }
}