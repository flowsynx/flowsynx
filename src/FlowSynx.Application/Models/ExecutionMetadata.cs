namespace FlowSynx.Application.Models;

public class ExecutionMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Namespace { get; set; } = "default";
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
    public DateTimeOffset CreatedAt { get; set; }
}