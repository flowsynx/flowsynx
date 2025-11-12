using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Email;

public class EmailTemplate
{
    private const string Template = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <title>Workflow Approval Required</title>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <style>
    body {{
      background-color: #f5f6fa;
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      color: #333;
      margin: 0;
      padding: 0;
    }}
    .container {{
      background-color: #ffffff;
      max-width: 600px;
      margin: 40px auto;
      padding: 24px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.05);
    }}
    h2 {{
      color: #2f3640;
      text-align: center;
      margin-bottom: 24px;
    }}
    .info {{
      font-size: 14px;
      line-height: 1.6;
      background: #f9f9f9;
      padding: 12px 16px;
      border-left: 4px solid #4a90e2;
      border-radius: 4px;
      margin-bottom: 20px;
    }}
    .info strong {{
      color: #222;
    }}
    .actions {{
      text-align: center;
      margin-top: 30px;
    }}
    .btn {{
      display: inline-block;
      padding: 12px 24px;
      margin: 0 10px;
      text-decoration: none;
      color: #fff;
      border-radius: 6px;
      font-weight: bold;
      transition: background 0.2s ease-in-out;
    }}
    .btn-approve {{
      background-color: #2ecc71;
    }}
    .btn-approve:hover {{
      background-color: #27ae60;
    }}
    .btn-reject {{
      background-color: #e74c3c;
    }}
    .btn-reject:hover {{
      background-color: #c0392b;
    }}
    .footer {{
      text-align: center;
      font-size: 12px;
      color: #999;
      margin-top: 30px;
    }}
    .note {{
      font-size: 13px;
      color: #555;
      background: #fcfcfc;
      border: 1px dashed #ddd;
      padding: 10px;
      border-radius: 6px;
      margin-top: 20px;
    }}
  </style>
</head>
<body>
  <div class=""container"">
    <h2>Workflow Approval Required</h2>
    <div class=""info"">
      <p><strong>Workflow Id:</strong> {{WorkflowId}}</p>
      <p><strong>Execution Id:</strong> {{ExecutionId}}</p>
      <p><strong>Task:</strong> {{TaskName}}</p>
      <p><strong>Requested By:</strong> {{RequestedBy}}</p>
      <p><strong>Requested At (UTC):</strong> {{RequestedAt}}</p>
    </div>

    <div class=""actions"">
      <a href=""{{ApproveUrl}}"" class=""btn btn-approve"">Approve</a>
      <a href=""{{RejectUrl}}"" class=""btn btn-reject"">Reject</a>
    </div>

    <div class=""note"">
      <strong>Note:</strong> These links target <code>POST</code> endpoints.<br>
      Use an API client or UI that issues POST requests.
    </div>

    <div class=""footer"">
      <p>This is an automated message from your Workflow Engine.</p>
    </div>
  </div>
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
            .Replace("{{ApproveUrl}}", approveUrl ?? "#")
            .Replace("{{RejectUrl}}", rejectUrl ?? "#");
    }
}