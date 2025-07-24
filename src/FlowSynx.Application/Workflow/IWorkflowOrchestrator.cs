using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowOrchestrator
{
    Task<WorkflowExecutionStatus> ExecuteWorkflowAsync(
        string userId, 
        Guid workflowId, 
        CancellationToken cancellationToken);

    Task<WorkflowExecutionStatus> ResumeWorkflowAsync(
        string userId,
        Guid workflowId,
        Guid executionId, 
        CancellationToken cancellationToken);
}