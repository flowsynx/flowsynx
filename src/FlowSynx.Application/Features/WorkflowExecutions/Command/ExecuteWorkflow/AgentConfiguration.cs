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

    /// <summary>
    /// Optional: Allow-list of tool names the agent is permitted to call.
    /// </summary>
    public List<string>? AllowTools { get; set; } = new();

    /// <summary>
    /// Optional: Deny-list of tool names the agent must not call.
    /// </summary>
    public List<string>? DenyTools { get; set; } = new();

    /// <summary>
    /// Optional: Maximum tool calls per agent run (safety cap).
    /// </summary>
    public int MaxToolCalls { get; set; } = 6;

    /// <summary>
    /// Optional: Whether tool calls require approval (integrates with manual approval patterns).
    /// </summary>
    public bool RequireToolApproval { get; set; }

    /// <summary>
    /// Optional: If true, the agent simulates tool effects and returns a plan (no side effects).
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Optional: Tool selection strategy hint for providers, e.g. "auto" | "required" | "none".
    /// </summary>
    public string? ToolSelection { get; set; } = "auto";
}