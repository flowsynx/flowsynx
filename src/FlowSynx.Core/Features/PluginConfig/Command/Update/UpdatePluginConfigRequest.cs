using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.PluginConfig.Command.Update;

public class UpdatePluginConfigRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}