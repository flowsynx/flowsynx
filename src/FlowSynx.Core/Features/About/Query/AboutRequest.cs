using MediatR;
using FlowSynx.Abstractions;

namespace FlowSynx.Core.Features.About.Query;

public class AboutRequest : IRequest<Result<object>>
{
    public string Entity { get; set; } = string.Empty;
    public FlowSynx.Connectors.Abstractions.Options? Options { get; set; } = new FlowSynx.Connectors.Abstractions.Options();
}