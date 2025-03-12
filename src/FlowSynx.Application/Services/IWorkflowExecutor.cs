namespace FlowSynx.Application.Services;

public interface IWorkflowExecutor
{
    Task ExecuteAsync(string workflowDefinition, CancellationToken cancellationToken);
}