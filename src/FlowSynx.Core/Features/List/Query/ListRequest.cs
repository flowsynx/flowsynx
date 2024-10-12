using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.List.Query;

public class ListRequest : IRequest<Result<IEnumerable<object>>>
{
    public required string Entity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();

}