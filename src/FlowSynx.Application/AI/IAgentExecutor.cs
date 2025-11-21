using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.AI;

public interface IAgentExecutor
{
    /// <summary>
    /// Executes a task using AI agent with the specified mode.
    /// </summary>
    Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionContext context,
        AgentConfiguration config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates if agent can handle the specified task type.
    /// </summary>
    Task<bool> CanHandleAsync(string taskType, CancellationToken cancellationToken);
}