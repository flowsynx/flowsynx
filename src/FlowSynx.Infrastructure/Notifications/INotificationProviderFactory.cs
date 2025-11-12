using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications;

public interface INotificationProviderFactory
{
    INotificationProvider Create(IEnumerable<string>? providerNames = null);
}
