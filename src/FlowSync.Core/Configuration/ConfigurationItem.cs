namespace FlowSync.Core.Configuration;

public class ConfigurationItem
{
    public ConfigurationItem(string name, string type)
    {
        Name = name;
        Type = type;
    }

    public required string Name { get; set; }
    public required string Type { get; set; }
    public object? Specifications { get; set; }
}