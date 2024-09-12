using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Read.Query;

public class ReadRequest : IRequest<Result<object>>
{
    public required string Entity { get; set; }
    public PluginOptions? Options { get; set; } = new PluginOptions();
}