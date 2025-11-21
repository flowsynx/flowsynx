namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class AgentConfiguration
{
    /// <summary>
    /// Enables AI agent for this task.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Agent mode: "execute" | "plan" | "validate" | "assist"
    /// </summary>
    public string Mode { get; set; } = "execute";

    /// <summary>
    /// Custom instructions/prompt for the agent.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Max iterations for agent reasoning loops.
    /// </summary>
    public int MaxIterations { get; set; } = 3;

    /// <summary>
    /// Temperature for LLM generation (0.0-1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.2;

    /// <summary>
    /// Additional context or constraints for the agent.
    /// </summary>
    public Dictionary<string, object>? Context { get; set; } = new();
}