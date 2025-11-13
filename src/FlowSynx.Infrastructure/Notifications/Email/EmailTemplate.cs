using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Email;

public static class EmailTemplate
{
    // Simplified, inline-styled template for better email client compatibility.
    private const string Template = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Workflow Approval Required</title>
</head>
<body style=""margin:0;padding:0;background-color:#f5f6fa;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color:#f5f6fa;padding:24px 0;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""max-width:600px;background-color:#ffffff;border-radius:8px;border:1px solid #e1e4e8;"">
          <tr>
            <td style=""padding:24px 24px 8px 24px;text-align:center;"">
              <h2 style=""margin:0;font-size:20px;color:#2f3640;font-weight:600;font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;"">
                Workflow Approval Required
              </h2>
            </td>
          </tr>
          <tr>
            <td style=""padding:8px 24px 0 24px;"">
              <div style=""font-size:14px;line-height:1.6;background:#f9f9f9;padding:12px 16px;border-left:4px solid #4a90e2;border-radius:4px;color:#333;"">
                <p style=""margin:0 0 8px 0;""><strong style=""color:#222;"">Comment:</strong> {{Comment}}</p>
                <p style=""margin:0 0 8px 0;""><strong style=""color:#222;"">Task:</strong> {{TaskName}}</p>
                <p style=""margin:0 0 8px 0;""><strong style=""color:#222;"">Workflow Id:</strong> {{WorkflowId}}</p>
                <p style=""margin:0 0 8px 0;""><strong style=""color:#222;"">Execution Id:</strong> {{ExecutionId}}</p>
                <p style=""margin:0 0 8px 0;""><strong style=""color:#222;"">Requested By:</strong> {{RequestedBy}}</p>
                <p style=""margin:0;""><strong style=""color:#222;"">Requested At (UTC):</strong> {{RequestedAt}}</p>
              </div>
            </td>
          </tr>
          <tr>
            <td style=""padding:24px;text-align:center;"">
              <a href=""{{ApproveUrl}}"" style=""display:inline-block;text-decoration:none;background:#2ecc71;color:#ffffff;padding:12px 20px;border-radius:6px;font-size:14px;font-weight:600;margin:0 6px;"">Approve</a>
              <a href=""{{RejectUrl}}"" style=""display:inline-block;text-decoration:none;background:#e74c3c;color:#ffffff;padding:12px 20px;border-radius:6px;font-size:14px;font-weight:600;margin:0 6px;"">Reject</a>
            </td>
          </tr>
          <tr>
            <td style=""padding:0 24px 16px 24px;"">
              <div style=""font-size:12px;color:#555;background:#fcfcfc;border:1px dashed #ddd;padding:10px;border-radius:6px;"">
                <strong>Note:</strong> Email links issue GET requests. If your approval API requires POST, route these links to a landing page or use a one-time token endpoint.
              </div>
            </td>
          </tr>
          <tr>
            <td style=""padding:0 24px 24px 24px;text-align:center;"">
              <p style=""margin:0;font-size:12px;color:#999;"">This is an automated message from FlowSynx Workflow Engine.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
";

    public static string Generate(NotificationApprovalMessage approvalMessage, string approveUrl, string rejectUrl)
    {
        if (approvalMessage == null)
            throw new ArgumentNullException(nameof(approvalMessage));

        return Template
            .Replace("{{WorkflowId}}", approvalMessage.WorkflowId.ToString())
            .Replace("{{ExecutionId}}", approvalMessage.ExecutionId.ToString())
            .Replace("{{TaskName}}", approvalMessage.TaskName ?? string.Empty)
            .Replace("{{RequestedBy}}", approvalMessage.RequestedBy ?? string.Empty)
            .Replace("{{RequestedAt}}", approvalMessage.RequestedAt.ToString("O"))
            .Replace("{{Comment}}", approvalMessage.Comment ?? string.Empty)
            .Replace("{{ApproveUrl}}", approveUrl ?? "#")
            .Replace("{{RejectUrl}}", rejectUrl ?? "#");
    }
}