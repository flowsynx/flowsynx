namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowExecutionContext
{
    public string UserId { get; }
    public Guid WorkflowId { get; }
    public Guid WorkflowExecutionId { get; }

    public WorkflowExecutionContext(string userId, Guid workflowId, Guid workflowExecutionId)
    {
        UserId = userId;
        WorkflowId = workflowId;
        WorkflowExecutionId = workflowExecutionId;
    }
}