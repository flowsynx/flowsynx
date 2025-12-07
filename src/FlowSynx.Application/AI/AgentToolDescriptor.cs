namespace FlowSynx.Application.AI;

public class AgentToolDescriptor
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, AgentToolParameter> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public bool Deterministic { get; init; }
    public bool SideEffecting { get; init; } = true;
}