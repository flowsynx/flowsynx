namespace FlowSynx.Application.AI;

public class AgentToolParameter
{
    public string Type { get; init; } = "string"; // string | number | boolean | object | array
    public bool Required { get; init; }
    public string? Description { get; init; }
    public object? Default { get; init; }
}