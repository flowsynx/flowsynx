using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Email;

public class EmailApprovalTemplate : INotificationTemplate
{
    public string GenerateTitle(NotificationApprovalMessage approval)
        => $"Approval required: Workflow {approval.WorkflowId} - Task '{approval.TaskName}'";

    public string GenerateBody(NotificationApprovalMessage approval, string approveUrl, string rejectUrl)
        => EmailTemplate.Generate(approval, approveUrl, rejectUrl);
}