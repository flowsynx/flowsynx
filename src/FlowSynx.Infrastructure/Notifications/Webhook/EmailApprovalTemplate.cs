using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Webhook;

public class WebhookApprovalTemplate : INotificationTemplate
{
    public string GenerateTitle(NotificationApprovalMessage approval)
        => $"Approval required: Workflow {approval.WorkflowId} - Task '{approval.TaskName}'";

    public string GenerateBody(NotificationApprovalMessage approval, string approveUrl, string rejectUrl)
        => WebhookTemplate.Generate(approval, approveUrl, rejectUrl);
}