using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadRequest : IRequest<Result<ReadResult>>
{
    public required string Entity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}