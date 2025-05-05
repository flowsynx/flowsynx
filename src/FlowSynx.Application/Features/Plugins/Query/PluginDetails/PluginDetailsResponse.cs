namespace FlowSynx.Application.Features.Plugins.Query.PluginDetails;

public class PluginDetailsResponse
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<PluginDetailsSpecification>? Specifications { get; set; } = new List<PluginDetailsSpecification>();
}