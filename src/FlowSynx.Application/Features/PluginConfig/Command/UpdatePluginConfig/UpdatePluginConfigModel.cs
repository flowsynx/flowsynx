namespace FlowSynx.Application.Features.PluginConfig.Command.UpdatePluginConfig;

public class UpdatePluginConfigModel
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Version { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}