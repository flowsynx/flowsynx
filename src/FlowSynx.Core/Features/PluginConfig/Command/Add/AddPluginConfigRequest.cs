using MediatR;
using FlowSynx.Core.Wrapper;

namespace FlowSynx.Core.Features.PluginConfig.Command.Add;

public class AddPluginConfigRequest : IRequest<Result<AddPluginConfigResponse>>
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}