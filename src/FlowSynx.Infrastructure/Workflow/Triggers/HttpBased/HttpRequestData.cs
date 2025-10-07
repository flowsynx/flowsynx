namespace FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;

public class HttpRequestData
{
    public string Method { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string? Body { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> Query { get; set; } = new Dictionary<string, string>();
}