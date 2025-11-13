using FlowSynx.Application.Notifications;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Notifications.Webhook;

public static class WebhookTemplate
{
    public static string Generate(NotificationApprovalMessage approvalMessage, string approveUrl, string rejectUrl)
    {
        if (approvalMessage == null)
            throw new ArgumentNullException(nameof(approvalMessage));

        // Markdown-friendly message for Slack / Teams / Discord
        var text = $@"
**Workflow Approval Required**

**Comment:** {approvalMessage.Comment}  
**Task:** {approvalMessage.TaskName}  
**Workflow Id:** {approvalMessage.WorkflowId}  
**Execution Id:** {approvalMessage.ExecutionId}  
**Requested By:** {approvalMessage.RequestedBy}  
**Requested At (UTC):** {approvalMessage.RequestedAt:O}  

**Actions:**
- ✅ [Approve]({approveUrl})
- ❌ [Reject]({rejectUrl})

> This is an automated message from your Workflow Engine.
";

        var payload = new
        {
            text = text.Trim()
        };

        return JsonSerializer.Serialize(payload);
    }
}