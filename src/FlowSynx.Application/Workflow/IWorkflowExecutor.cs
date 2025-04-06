namespace FlowSynx.Application.Workflow;

public interface IWorkflowExecutor
{
    Task ExecuteAsync(string userId, Guid workflowId, CancellationToken cancellationToken);
}