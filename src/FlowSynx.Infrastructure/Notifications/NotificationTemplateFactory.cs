using FlowSynx.Application.Configuration.Integrations.Notifications;
using FlowSynx.Application.Notifications;
using FlowSynx.Infrastructure.Notifications.Email;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Notifications;

public class NotificationTemplateFactory : INotificationTemplateFactory
{
    private readonly NotificationsConfiguration _config;
    private readonly ILogger<NotificationTemplateFactory> _logger;

    public NotificationTemplateFactory(
        NotificationsConfiguration config,
        ILogger<NotificationTemplateFactory> logger)
    {
        _config = config;
        _logger = logger;
    }

    public INotificationTemplate GetTemplate(string providerName)
    {
        if (!_config.Enabled)
            throw new InvalidOperationException("Notifications are disabled.");

        if (!_config.Providers.TryGetValue(providerName, out var providerConfig))
            throw new KeyNotFoundException($"Notification provider '{providerName}' is not configured.");

        _logger.LogInformation("Creating notification template for {ProviderName} ({Type})", providerName, providerConfig.Type);

        return providerConfig switch
        {
            EmailConfiguration => new EmailApprovalTemplate(),
            _ => throw new NotSupportedException($"Notification type '{providerConfig.Type}' not supported")
        };
    }
}