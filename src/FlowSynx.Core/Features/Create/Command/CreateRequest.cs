using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Create.Command;

public class CreateRequest : IRequest<Result<Unit>>
{
    public required string Entity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}