using MediatR;
using FlowSync.Core.Common.Models;

namespace FlowSync.Core.Features.Config.Query.List;

public class ConfigListRequest : IRequest<Result<IEnumerable<ConfigListResponse>>>
{
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? Sorting { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public int? MaxResults { get; set; } = 10;
}