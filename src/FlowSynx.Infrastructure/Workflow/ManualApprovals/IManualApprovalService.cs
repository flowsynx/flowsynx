using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Infrastructure.Workflow.ManualApprovals;

public interface IManualApprovalService
{
    Task RequestApprovalAsync(
        WorkflowExecutionEntity execution, 
        ManualApproval? approvalConfig, 
        CancellationToken cancellationToken);

    Task ApproveAsync(
        Guid executionId, 
        string approver, 
        CancellationToken cancellationToken);

    Task RejectAsync(
        Guid executionId, 
        string approver, 
        CancellationToken cancellationToken);

    Task<WorkflowApprovalStatus> GetApprovalStatusAsync(
        Guid executionId, 
        CancellationToken cancellationToken);
}