namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecutionQueueRequest
{
    public string UserId { get; }
    public Guid WorkflowId { get; }
    public Guid ExecutionId { get; }
    public CancellationToken CancellationToken { get; }

    public ExecutionQueueRequest(
        string userId, 
        Guid workflowId, 
        Guid executionId, 
        CancellationToken cancellationToken)
    {
        UserId = userId;
        WorkflowId = workflowId;
        ExecutionId = executionId;
        CancellationToken = cancellationToken;
    }
}