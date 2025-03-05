namespace FlowSynx.Core.Features.Plugins.Query.Details;

public class PluginDetailsResponse
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<PluginDetailsSpecification>? Specifications { get; set; } = new List<PluginDetailsSpecification>();
}