using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Delete.Command;

public class DeleteRequest : IRequest<Result<Unit>>
{
    public required string Entity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}