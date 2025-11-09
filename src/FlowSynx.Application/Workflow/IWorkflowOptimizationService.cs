using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowOptimizationService
{
    /// <summary>
    /// Applies safe, explainable optimizations (e.g., concurrency, timeouts) to a WorkflowDefinition.
    /// </summary>
    Task<(WorkflowDefinition Optimized, string Explanation)> OptimizeAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken);
}