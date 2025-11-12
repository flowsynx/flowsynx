namespace FlowSynx.Application.Notifications;

public interface INotificationTemplate
{
    string GenerateBody(NotificationApprovalMessage approval, string approveUrl, string rejectUrl);
    string GenerateTitle(NotificationApprovalMessage approval);
}