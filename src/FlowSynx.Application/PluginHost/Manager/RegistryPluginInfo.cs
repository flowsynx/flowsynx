namespace FlowSynx.Application.PluginHost.Manager;

public sealed class RegistryPluginInfo
{
    public string Type { get; init; } = default!;
    public string CategoryTitle { get; init; } = default!;
    public string? Description { get; init; }
    public string Version { get; init; } = default!;
    public string Registry { get; init; } = default!;
}