using FlowSynx.Domain.Entities.PluginConfig;

namespace FlowSynx.Domain.Entities.Plugin;

public class PluginEntity : AuditableEntity<Guid>, ISoftDeletable
{
    public required Guid PluginId { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public required string Type { get; set; }
    public required string PluginLocation { get; set; }
    public required string Checksum { get; set; }
    public List<PluginSpecification>? Specifications { get; set; } = new List<PluginSpecification>();
    public bool IsDeleted { get; set; } = false;

    public List<PluginConfigurationEntity> PluginConfigurations { get; set; } = new();
}