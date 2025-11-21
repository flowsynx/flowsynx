namespace FlowSynx.Application.AI;

public class AgentExecutionContext
{
    public required string TaskName { get; set; }
    public required object TaskType { get; set; }
    public required Dictionary<string, object?> TaskParameters { get; set; }
    public string? TaskDescription { get; set; }
    public Dictionary<string, object?>? WorkflowVariables { get; set; } = new();
    public Dictionary<string, object>? PreviousTaskOutputs { get; set; } = new();
    public string? UserInstructions { get; set; }
    public Dictionary<string, object>? AdditionalContext { get; set; } = new();
}