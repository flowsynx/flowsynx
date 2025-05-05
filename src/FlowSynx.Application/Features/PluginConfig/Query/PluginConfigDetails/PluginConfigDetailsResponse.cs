namespace FlowSynx.Application.Features.PluginConfig.Query.PluginConfigDetails;

public class PluginConfigDetailsResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Version { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}