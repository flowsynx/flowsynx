using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.List.Query;

public class ListRequest : IRequest<Result<IEnumerable<object>>>
{
    public required string Entity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();

}