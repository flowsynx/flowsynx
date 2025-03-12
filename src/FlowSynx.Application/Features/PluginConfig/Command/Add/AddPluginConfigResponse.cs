namespace FlowSynx.Application.Features.PluginConfig.Command.Add;

public class AddPluginConfigResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}