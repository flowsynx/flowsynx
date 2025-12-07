namespace FlowSynx.Application.AI;

public class AgentExecutionResult
{
    public bool Success { get; set; }
    public object? Output { get; set; }
    public string? Reasoning { get; set; }
    public List<string> Steps { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public List<AgentStep> Trace { get; set; } = new();
}
