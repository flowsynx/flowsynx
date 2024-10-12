using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Connectors.Query.Details;

public class ConnectorDetailsRequest : IRequest<Result<ConnectorDetailsResponse>>
{
    public required string Type { get; set; }
}