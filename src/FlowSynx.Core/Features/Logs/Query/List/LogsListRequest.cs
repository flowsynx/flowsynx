using FlowSynx.Abstractions;
using MediatR;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListRequest : IRequest<Result<IEnumerable<object>>>
{
    public string? Fields { get; set; }
    public string? Filters { get; set; }
    public string? Sorts { get; set; }
    public string? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}

