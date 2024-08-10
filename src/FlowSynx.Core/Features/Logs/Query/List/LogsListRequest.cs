using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<LogsListResponse>>>
{
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? Level { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? MaxResults { get; set; }
    public string? Sorting { get; set; }
}

