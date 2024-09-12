using MediatR;
using FlowSynx.Abstractions;
using FlowSynx.Plugin.Abstractions;

namespace FlowSynx.Core.Features.Move.Command;

public class MoveRequest : IRequest<Result<IEnumerable<object>>>
{
    public required string SourceEntity { get; set; }
    public required string DestinationEntity { get; set; }
    public PluginOptions? Options { get; set; } = new PluginOptions();
}