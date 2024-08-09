using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Plugins.Query.List;

public class PluginsListRequest : IRequest<Result<IEnumerable<PluginsListResponse>>>
{
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? MaxResults { get; set; }
    public string? Sorting { get; set; }
}