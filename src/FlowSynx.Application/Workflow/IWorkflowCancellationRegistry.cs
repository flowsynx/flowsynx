namespace FlowSynx.Application.Workflow;

public interface IWorkflowCancellationRegistry
{
    CancellationToken Register(string userId, Guid workflowId, Guid workflowExecutionId);
    void Cancel(string userId, Guid workflowId, Guid workflowExecutionId);
    void Remove(string userId, Guid workflowId, Guid workflowExecutionId);
    bool IsRegistered(string userId, Guid workflowId, Guid workflowExecutionId);
}