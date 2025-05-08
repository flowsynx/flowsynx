namespace FlowSynx.Application.Workflow;

public interface IWorkflowOrchestrator
{
    Task ExecuteWorkflowAsync(string userId, Guid workflowId, CancellationToken cancellationToken);
}