using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Workflow;

public interface IWorkflowOrchestrator
{
    Task<WorkflowExecutionEntity> CreateWorkflowExecutionAsync(
        string userId,
        Guid workflowId,
        CancellationToken cancellationToken);

    Task<WorkflowExecutionStatus> ExecuteWorkflowAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        WorkflowTrigger? trigger,
        CancellationToken cancellationToken);

    Task<WorkflowExecutionStatus> ResumeWorkflowAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        CancellationToken cancellationToken);
}
