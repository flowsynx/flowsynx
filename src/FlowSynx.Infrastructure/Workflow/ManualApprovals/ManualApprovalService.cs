using FlowSynx.Application.Configuration.Integrations.Notifications;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Application.Notifications;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Notifications;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ManualApprovals;

public class ManualApprovalService: IManualApprovalService
{
    private readonly IWorkflowApprovalService _workflowApprovalService;
    private readonly ILogger<ManualApprovalService> _logger;
    private readonly INotificationProviderFactory _notificationProviderFactory;
    private readonly INotificationTemplateFactory _notificationTemplateFactory;
    private readonly NotificationsConfiguration _notifConfig = new();

    public ManualApprovalService(
        IWorkflowApprovalService workflowApprovalService,
        ILogger<ManualApprovalService> logger,
        INotificationProviderFactory notificationProviderFactory,
        INotificationTemplateFactory notificationTemplateFactory,
        NotificationsConfiguration notifOptions)
    {
        _workflowApprovalService = workflowApprovalService ?? throw new ArgumentNullException(nameof(workflowApprovalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationProviderFactory = notificationProviderFactory ?? throw new ArgumentNullException(nameof(notificationProviderFactory));
        _notificationTemplateFactory = notificationTemplateFactory ?? throw new ArgumentNullException(nameof(notificationTemplateFactory));
        _notifConfig = notifOptions ?? new NotificationsConfiguration();
    }

    public async Task RequestApprovalAsync(
        WorkflowExecutionEntity execution,
        ManualApproval? manualApproval,
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
            Comment = manualApproval?.Comment ?? "",
            Status = WorkflowApprovalStatus.Pending,
        };

        await _workflowApprovalService.AddAsync(approvalEntity, cancellationToken);
        _logger.LogInformation("Approval requested for workflow execution {ExecutionId}", execution.Id);

        await TrySendApprovalNotificationAsync(execution, approvalEntity, cancellationToken);
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

    private async Task TrySendApprovalNotificationAsync(
        WorkflowExecutionEntity execution,
        WorkflowApprovalEntity approval,
        CancellationToken cancellationToken)
    {
        var baseUrl = _notifConfig.BaseUrl?.TrimEnd('/');
        var approveUrl = baseUrl is null
            ? $"/workflows/{approval.WorkflowId}/executions/{approval.ExecutionId}/approvals/{approval.Id}/approve"
            : $"{baseUrl}/workflows/{approval.WorkflowId}/executions/{approval.ExecutionId}/approvals/{approval.Id}/approve";

        var rejectUrl = baseUrl is null
            ? $"/workflows/{approval.WorkflowId}/executions/{approval.ExecutionId}/approvals/{approval.Id}/reject"
            : $"{baseUrl}/workflows/{approval.WorkflowId}/executions/{approval.ExecutionId}/approvals/{approval.Id}/reject";

        var approvalMessage = new NotificationApprovalMessage
        {
            WorkflowId = approval.WorkflowId,
            ExecutionId = approval.ExecutionId,
            TaskName = approval.TaskName,
            RequestedBy = approval.RequestedBy,
            RequestedAt = approval.RequestedAt,
            Comment = approval.Comment,
        };

        try
        {
            foreach (var providerKey in _notifConfig.DefaultProviders)
            {
                var provider = _notificationProviderFactory.CreateProvider(providerKey);
                var template = _notificationTemplateFactory.GetTemplate(providerKey);

                var subject = template.GenerateTitle(approvalMessage);
                var body = template.GenerateBody(approvalMessage, approveUrl, rejectUrl);

                var notificationMessage = new NotificationMessage
                {
                    Title = subject,
                    Body = body,
                    Metadata = new Dictionary<string, string>
                {
                    { "WorkflowId", approval.WorkflowId.ToString() },
                    { "ExecutionId", approval.ExecutionId.ToString() },
                    { "ApprovalId", approval.Id.ToString() },
                    { "TaskName", approval.TaskName }
                }
                };

                await provider.SendAsync(notificationMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send workflow approval email for Execution {ExecutionId}", execution.Id);
        }
    }
}