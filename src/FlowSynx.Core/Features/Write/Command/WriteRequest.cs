using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteRequest : IRequest<Result<Unit>>
{
    public required string Entity { get; set; }
    public required object Data { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}