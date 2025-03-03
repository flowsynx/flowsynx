namespace FlowSynx.Core.Features.Config.Query.Details;

public class PluginConfigDetailsResponse
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, string?>? Specifications { get; set; }
}