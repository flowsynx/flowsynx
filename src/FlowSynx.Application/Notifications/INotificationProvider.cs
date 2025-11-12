namespace FlowSynx.Application.Notifications;

public interface INotificationProvider
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}