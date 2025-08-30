namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecuteWorkflowResponse
{
    public required Guid WorkflowId { get; set; }
    public required Guid ExecutionId { get; set; }
    public required DateTime StartedAt { get; set; }
}