using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ManualApprovals;

public class ManualApprovalService: IManualApprovalService
{
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly ILogger<ManualApprovalService> _logger;

    public ManualApprovalService(
        IWorkflowApprovalService workflowApprovalService,
        ILogger<ManualApprovalService> logger)
    {
        _workflowApprovalService = workflowApprovalService ?? throw new ArgumentNullException(nameof(workflowApprovalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    }

    public async Task ApproveAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        Guid approvalId,
        CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetAsync(userId,  workflowId, executionId, approvalId, cancellationToken);
        if (approval == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowApprovalNotFound, "Approval request not found.");

        if (approval.Status != WorkflowApprovalStatus.Pending)
            throw new FlowSynxException((int)ErrorCode.WorkflowAlreadyApprovedOrRejected, "Already approved or rejected.");

        approval.Status = WorkflowApprovalStatus.Approved;
        approval.Approver = userId;
        approval.DecidedAt = DateTime.UtcNow;

        await _workflowApprovalService.UpdateAsync(approval, cancellationToken);
        _logger.LogInformation("Workflow execution {ExecutionId} approved by '{Approver}'", executionId, userId);
    }

    public async Task RejectAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        Guid approvalId,
        CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetAsync(userId, workflowId, executionId, approvalId, cancellationToken);
        if (approval == null)
            throw new FlowSynxException((int)ErrorCode.WorkflowApprovalNotFound, "Approval request not found.");

        if (approval.Status != WorkflowApprovalStatus.Pending)
            throw new FlowSynxException((int)ErrorCode.WorkflowAlreadyApprovedOrRejected, "Already approved or rejected.");

        approval.Status = WorkflowApprovalStatus.Rejected;
        approval.Approver = userId;
        approval.DecidedAt = DateTime.UtcNow;

        await _workflowApprovalService.UpdateAsync(approval, cancellationToken);
        _logger.LogInformation("Workflow execution {ExecutionId} rejected by '{Approver}'", executionId, userId);
    }

    public async Task<WorkflowApprovalStatus> GetApprovalStatusAsync(
        string userId,
        Guid workflowId,
        Guid executionId,
        string taskName,
        CancellationToken cancellationToken)
    {
        var approval = await _workflowApprovalService.GetByTaskNameAsync(userId, workflowId, executionId, taskName, cancellationToken);

        return approval == null ? WorkflowApprovalStatus.Pending : approval.Status;
    }
}
