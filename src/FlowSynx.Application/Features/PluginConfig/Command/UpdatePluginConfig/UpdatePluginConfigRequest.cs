using MediatR;
using FlowSynx.Application.Wrapper;

namespace FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;

public class UpdatePluginConfigRequest : IRequest<Result<Unit>>
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Version { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}