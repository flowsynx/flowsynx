using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.AI;

public interface IAiProvider
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Given a business goal and (optionally) a capabilities catalog, returns a JSON string
    /// that matches FlowSynx workflow schema. The handler validates it before use.
    /// </summary>
    Task<string> GenerateWorkflowJsonAsync(
        string goal,
        string? capabilitiesJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes an agentic task with reasoning and tool use.
    /// </summary>
    Task<AgentExecutionResult> ExecuteAgenticTaskAsync(
        AgentExecutionContext context,
        AgentConfiguration config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Plans execution strategy for a task.
    /// </summary>
    Task<string> PlanTaskExecutionAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates task parameters and outputs.
    /// </summary>
    Task<(bool IsValid, string? ValidationMessage)> ValidateTaskAsync(
        AgentExecutionContext context,
        object? output,
        CancellationToken cancellationToken);
}