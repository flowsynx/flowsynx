using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Transfer.Command;

public class TransferRequest : IRequest<Result<Unit>>
{
    public required BaseRequest From { get; set; }
    public required BaseRequest To { get; set; }
    public string? TransferKind { get; set; } = FlowSynx.Connectors.Abstractions.TransferKind.Copy.ToString();
}