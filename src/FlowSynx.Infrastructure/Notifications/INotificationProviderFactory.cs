using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications;

public interface INotificationProviderFactory
{
    INotificationProvider CreateProvider(string providerName);
}
