namespace FlowSynx.Application.Notifications;

public interface INotificationProvider
{
    //string Name { get; }
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}