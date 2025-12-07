namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTask(string name)
{
    /// <summary>
    /// Gets or sets the name of the workflow task.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the description of the workflow task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type identifier associated with the current task.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets the configuration settings related to execution behavior.
    /// </summary>
    public ExecutionConfig Execution { get; init; } = new();

    /// <summary>
    /// Gets the configuration settings related to flow control and dependencies.
    /// </summary>
    public FlowControlConfig FlowControl { get; init; } = new();

    /// <summary>
    /// Gets or sets the error handling strategy to use for this operation.
    public ErrorHandling? ErrorHandling { get; set; }

    /// <summary>
    /// Gets or sets the manual approval configuration for the operation.
    /// </summary>
    public ManualApproval? ManualApproval { get; set; }

    /// <summary>
    /// Gets or sets the output configuration for the operation.
    /// </summary>
    public string? Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position of the workflow task within the overall workflow.
    /// </summary>
    public WorkflowTaskPosition? Position { get; set; } = new(0, 0);
}