namespace FlowSynx.Domain.Plugin;

public class PluginSpecification
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? DeclaringType { get; set; }
    public bool IsReadable { get; set; }
    public bool IsWritable { get; set; }
    public bool IsRequired { get; set; } = false;
}