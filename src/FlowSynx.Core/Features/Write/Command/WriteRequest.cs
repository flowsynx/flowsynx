using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Write.Command;

public class WriteRequest : IRequest<Result<Unit>>
{
    public required string Entity { get; set; }
    public required object Data { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}