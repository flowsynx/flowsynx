namespace FlowSynx.Infrastructure.PluginHost.Manager;

public sealed class RegistryPluginItem
{
    public string Type { get; set; } = default!;
    public string CategoryTitle { get; set; } = default!;
    public string? Description { get; set; }
    public string? Version { get; set; }
}