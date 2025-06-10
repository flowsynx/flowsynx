namespace FlowSynx.Domain.Plugin;

public class PluginEntity : AuditableEntity<Guid>, ISoftDeletable
{
    public required string UserId { get; set; }
    public required string Type { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public string? License { get; set; }
    public string? LicenseUrl { get; set; }
    public string? Icon { get; set; }
    public string? ProjectUrl { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? Copyright { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Owners { get; set; } = new List<string>();
    public string? Checksum { get; set; }
    public required string PluginLocation { get; set; }
    public List<PluginSpecification>? Specifications { get; set; } = new();
    public bool IsDeleted { get; set; } = false;
}