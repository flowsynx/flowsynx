using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.Exist.Query;

public class ExistRequest : IRequest<Result<object>>
{
    public required string Entity { get; set; }
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}