namespace FlowSynx.PluginCore;

public class PluginMetadata
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required PluginVersion Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public required PluginNamespace Namespace { get; set; }
    public string Type => $"FlowSynx.{Namespace}/{Name}";
}