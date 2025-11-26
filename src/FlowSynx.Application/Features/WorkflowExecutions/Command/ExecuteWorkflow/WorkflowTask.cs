namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTask(string name)
{
    // ─────────────────────────────────────────────
    // Core Identification
    // ─────────────────────────────────────────────
    public string Name { get; set; } = name;
    public string? Description { get; set; }
    public object? Type { get; set; }

    // ─────────────────────────────────────────────
    // Execution Configuration
    // ─────────────────────────────────────────────
    public Dictionary<string, object?>? Parameters { get; set; } = new();
    public AgentConfiguration? Agent { get; set; }
    public int? TimeoutMilliseconds { get; set; }

    // ─────────────────────────────────────────────
    // Flow Control and Dependencies
    // ─────────────────────────────────────────────
    public List<string> Dependencies { get; set; } = new();
    public List<string> RunOnFailureOf { get; set; } = new();
    public Condition? ExecutionCondition { get; set; }
    public List<ConditionalBranch> ConditionalBranches { get; set; } = new();

    // ─────────────────────────────────────────────
    // Error and Approval Handling
    // ─────────────────────────────────────────────
    public ErrorHandling? ErrorHandling { get; set; }
    public ManualApproval? ManualApproval { get; set; }

    // ─────────────────────────────────────────────
    // Output and Visualization
    // ─────────────────────────────────────────────
    public string? Output { get; set; } = string.Empty;
    public WorkflowTaskPosition? Position { get; set; } = new(0, 0);
}