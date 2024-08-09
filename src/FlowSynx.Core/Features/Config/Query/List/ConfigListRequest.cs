using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Config.Query.List;

public class ConfigListRequest : IRequest<Result<IEnumerable<ConfigListResponse>>>
{
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? MaxResults { get; set; }
    public string? Sorting { get; set; }
}