namespace FlowSynx.Domain.Workflows;

public class ActivityReference
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "latest";
    public string Namespace { get; set; } = "default";
}