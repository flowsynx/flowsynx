using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferRequest : IRequest<Result<Unit>>
{
    public required string SourceEntity { get; set; }
    public required string DestinationEntity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}