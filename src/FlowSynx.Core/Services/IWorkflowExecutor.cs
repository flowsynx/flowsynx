namespace FlowSynx.Core.Services;

public interface IWorkflowExecutor
{
    Task ExecuteAsync(string workflowDefinition, CancellationToken cancellationToken);
}