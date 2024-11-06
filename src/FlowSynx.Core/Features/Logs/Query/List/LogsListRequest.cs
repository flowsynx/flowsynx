using FlowSynx.Abstractions;
using FlowSynx.Data.Filter;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<object>>>
{
    public string[]? Fields { get; set; }
    public string? Filter { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public Sort[]? Sort { get; set; }
    public string? Limit { get; set; }
}

