using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferRequest : IRequest<Result<Unit>>
{
    public required BaseRequest Source { get; set; }
    public required BaseRequest Destination { get; set; }
}