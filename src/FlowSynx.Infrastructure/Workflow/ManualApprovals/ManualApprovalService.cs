using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ManualApprovals;

public class ManualApprovalService: IManualApprovalService
{
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly ILogger<ManualApprovalService> _logger;
    //private readonly INotificationService _notificationService; // Optional: Send emails/UI notifications

    public ManualApprovalService(
        IWorkflowApprovalService workflowApprovalService,
        ILogger<ManualApprovalService> logger
        //INotificationService notificationService
        )
    {
        _workflowApprovalService = workflowApprovalService ?? throw new ArgumentNullException(nameof(workflowApprovalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        //_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task RequestApprovalAsync(
        WorkflowExecutionEntity execution,
        ManualApproval? approvalConfig,
        CancellationToken cancellationToken)
    {
        var approvalEntity = new WorkflowApprovalEntity
        {
            Id = Guid.NewGuid(),
            UserId = execution.UserId,
            ExecutionId = execution.Id,
            WorkflowId = execution.WorkflowId,
            TaskName = execution.PausedAtTask!,
            RequestedBy = execution.UserId,
            RequestedAt = DateTime.UtcNow,
            Status = WorkflowApprovalStatus.Pending,
        };

        await _workflowApprovalService.AddAsync(approvalEntity, cancellationToken);
        _logger.LogInformation("Approval requested for workflow execution {ExecutionId}", execution.Id);

        //// Notify approvers (optional)
        //await _notificationService.SendApprovalRequestAsync(approvalEntity, approval, cancellationToken);
    }

    public async Task ApproveAsync(Guid executionId, string approver, CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetByExecutionIdAsync(executionId, cancellationToken);
        if (approval == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowApprovalNotFound, "Approval request not found.");

        if (approval.Status != WorkflowApprovalStatus.Pending)
            throw new FlowSynxException((int)ErrorCode.WorkflowAlreadyApprovedOrRejected, "Already approved or rejected.");

        approval.Status = WorkflowApprovalStatus.Approved;
        approval.Approver = approver;
        approval.ApprovedAt = DateTime.UtcNow;

        await _workflowApprovalService.UpdateAsync(approval, cancellationToken);
        _logger.LogInformation("Workflow execution {ExecutionId} approved by {Approver}", executionId, approver);
    }

    public async Task RejectAsync(Guid executionId, string approver, CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetByExecutionIdAsync(executionId, cancellationToken);
        if (approval == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowApprovalNotFound, "Approval request not found.");

        if (approval.Status != WorkflowApprovalStatus.Pending)
            throw new FlowSynxException((int)ErrorCode.WorkflowAlreadyApprovedOrRejected, "Already approved or rejected.");

        approval.Status = WorkflowApprovalStatus.Rejected;
        approval.Approver = approver;
        approval.ApprovedAt = DateTime.UtcNow;

        await _workflowApprovalService.UpdateAsync(approval, cancellationToken);
        _logger.LogInformation("Workflow execution {ExecutionId} rejected by {Approver}", executionId, approver);
    }

    public async Task<WorkflowApprovalStatus> GetApprovalStatusAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetByExecutionIdAsync(executionId, cancellationToken);
        if (approval == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowApprovalNotFound, "Approval request not found.");
        return approval.Status;
    }
}