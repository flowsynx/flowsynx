namespace FlowSynx.Core.Features.Workflow;

public interface IWorkflowExecutor
{
    Task<Dictionary<string, object?>> ExecuteAsync(WorkflowExecutionDefinition executionDefinition,
        CancellationToken cancellationToken);
}