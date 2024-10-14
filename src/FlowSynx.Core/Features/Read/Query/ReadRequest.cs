using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadRequest : IRequest<Result<ReadResult>>
{
    public required string Entity { get; set; }
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}