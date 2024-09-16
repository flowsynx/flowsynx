using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Plugins.Query.List;

public class PluginsListRequest : IRequest<Result<IEnumerable<object>>>
{
    public string[]? Fields { get; set; }
    public string? Filter { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? Sort { get; set; }
    public string? Limit { get; set; }
}