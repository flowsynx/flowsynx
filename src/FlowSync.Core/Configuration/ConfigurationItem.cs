namespace FlowSync.Core.Configuration;

public class ConfigurationItem
{
    public ConfigurationItem(Guid id, string name, string type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public object? Specifications { get; set; }
}