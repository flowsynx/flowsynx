using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Create.Command;

public class CreateRequest : IRequest<Result<Unit>>
{
    public required string Entity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}