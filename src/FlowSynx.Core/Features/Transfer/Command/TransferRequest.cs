using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferRequest : IRequest<Result<Unit>>
{
    public required string SourceEntity { get; set; }
    public required string DestinationEntity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}