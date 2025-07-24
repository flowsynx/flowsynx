using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Application.Workflow;

public interface IManualApprovalService
{
    Task RequestApprovalAsync(
        WorkflowExecutionEntity execution, 
        ManualApproval? approvalConfig, 
        CancellationToken cancellationToken);

    Task ApproveAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        Guid approvalId,
        CancellationToken cancellationToken);

    Task RejectAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        Guid approvalId,
        CancellationToken cancellationToken);

    Task<WorkflowApprovalStatus> GetApprovalStatusAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        string taskName,
        CancellationToken cancellationToken);
}