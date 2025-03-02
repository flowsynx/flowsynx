using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Domain.Interfaces;

public interface IWorkflowExecutor
{
    Task<Dictionary<string, object?>> ExecuteAsync(WorkflowExecutionDefinition executionDefinition,
        CancellationToken cancellationToken);
}