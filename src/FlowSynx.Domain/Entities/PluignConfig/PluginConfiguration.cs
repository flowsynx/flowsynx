namespace FlowSynx.Domain.Entities.PluignConfig;

public class PluginConfiguration : AuditableEntity, IEquatable<PluginConfiguration>
{
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public PluginConfigurationSpecifications? Specifications { get; set; }

    public override string ToString()
    {
        return $"{UserId}@{Type}:{Name}";
    }

    public bool Equals(PluginConfiguration? other)
    {
        if (other == null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (Name != other.Name) return false;
        if (UserId != other.UserId) return false;

        return true;
    }
}