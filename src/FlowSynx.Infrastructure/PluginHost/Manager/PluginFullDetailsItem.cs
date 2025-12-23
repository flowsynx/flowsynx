namespace FlowSynx.Infrastructure.PluginHost.Manager;

public sealed class PluginFullDetailsItem
{
    public string Type { get; set; } = default!;
    public string CategoryTitle { get; set; } = default!;
    public string? Description { get; set; }
    public IEnumerable<string> Versions { get; set; } = new List<string>();
    public string LatestVersion { get; set; } = string.Empty;
}