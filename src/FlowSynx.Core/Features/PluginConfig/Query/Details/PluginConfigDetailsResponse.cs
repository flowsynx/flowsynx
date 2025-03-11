namespace FlowSynx.Core.Features.PluginConfig.Query.Details;

public class PluginConfigDetailsResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object?>? Specifications { get; set; }
}