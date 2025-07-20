namespace FlowSynx.Application.Workflow;

public interface IWorkflowOrchestrator
{
    Task ExecuteWorkflowAsync(string userId, Guid workflowId, CancellationToken cancellationToken);
    Task ResumeWorkflowAsync(string userId, Guid executionId, Guid workflowId, CancellationToken cancellationToken);
}