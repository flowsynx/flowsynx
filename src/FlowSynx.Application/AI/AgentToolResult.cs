namespace FlowSynx.Application.AI;

public class AgentToolResult
{
    public bool Success { get; init; }
    public object? Output { get; init; }
    public string? ErrorMessage { get; init; }

    public static AgentToolResult Ok(object? output) => new() { Success = true, Output = output };
    public static AgentToolResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}