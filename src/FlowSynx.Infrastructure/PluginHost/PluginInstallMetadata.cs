namespace FlowSynx.Infrastructure.PluginHost;

public class PluginInstallMetadata
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public required string Type { get; set; }
    public required string Url { get; set; }
    public required string Checksum { get; set; } // SHA256 checksum
}
