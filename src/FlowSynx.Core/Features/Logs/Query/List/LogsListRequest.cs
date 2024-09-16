using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<object>>>
{
    public string[]? Fields { get; set; }
    public string? Filter { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public string? Sort { get; set; }
    public string? Limit { get; set; }
}

