using FlowSynx.Application.Notifications;

namespace FlowSynx.Infrastructure.Notifications;

public interface INotificationTemplateFactory
{
    INotificationTemplate GetTemplate(string providerName);
}