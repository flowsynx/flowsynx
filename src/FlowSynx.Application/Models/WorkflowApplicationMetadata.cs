namespace FlowSynx.Application.Models;

public class WorkflowApplicationMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default";
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset CreatedAt { get; set; }
    public bool Shared { get; set; } = false;
    public string Owner { get; set; } = string.Empty;
}