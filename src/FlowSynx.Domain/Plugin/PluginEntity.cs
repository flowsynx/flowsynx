﻿namespace FlowSynx.Domain.Plugin;

public class PluginEntity : AuditableEntity<Guid>, ISoftDeletable
{
    public required Guid PluginId { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public required string Type { get; set; }
    public required string PluginLocation { get; set; }
    public required string Checksum { get; set; }
    public List<PluginSpecification>? Specifications { get; set; } = new();
    public bool IsDeleted { get; set; } = false;
}