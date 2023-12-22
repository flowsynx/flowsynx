using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Plugins.Query.List;

public class PluginsListRequest : IRequest<Result<IEnumerable<PluginsListResponse>>>
{
    public string? Namespace { get; set; }
}