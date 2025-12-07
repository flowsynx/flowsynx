namespace FlowSynx.Application.AI;

public class AgentStep
{
    public string? Thought { get; set; }
    public string? Action { get; set; } // tool name
    public string? Operation { get; set; }
    public Dictionary<string, object?>? Args { get; set; }
    public object? Observation { get; set; }
    public string? Info { get; set; }
}