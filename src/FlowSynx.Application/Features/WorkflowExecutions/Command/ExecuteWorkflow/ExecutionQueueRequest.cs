namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecutionQueueRequest(string userId, Guid workflowId, Guid executionId)
{
    public string UserId { get; } = userId;
    public Guid WorkflowId { get; } = workflowId;
    public Guid ExecutionId { get; } = executionId;
}