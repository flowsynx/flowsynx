using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Connectors.Query.List;

public class ConnectorListRequest : IRequest<Result<IEnumerable<object>>>
{
    public string? Fields { get; set; }
    public string? Filters { get; set; }
    public string? Sorts { get; set; }
    public string? Paging { get; set; }
    public bool? CaseSensitive { get; set; } = false;
}