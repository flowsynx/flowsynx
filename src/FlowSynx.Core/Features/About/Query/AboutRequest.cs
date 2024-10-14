using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.About.Query;

public class AboutRequest : IRequest<Result<object>>
{
    public string Entity { get; set; } = string.Empty;
    public ConnectorOptions? Options { get; set; } = new ConnectorOptions();
}