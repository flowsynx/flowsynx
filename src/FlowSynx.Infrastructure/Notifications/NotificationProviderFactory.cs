using FlowSynx.Application.Configuration.Integrations.Notifications;
using FlowSynx.Application.Notifications;
using FlowSynx.Infrastructure.Notifications.Email;
using FlowSynx.Infrastructure.Notifications.Webhook;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Notifications;

public class NotificationProviderFactory: INotificationProviderFactory
{
    private readonly NotificationsConfiguration _config;
    private readonly ILogger<NotificationProviderFactory> _logger;

    public NotificationProviderFactory(
        NotificationsConfiguration config,
        ILogger<NotificationProviderFactory> logger)
    {
        _config = config;
        _logger = logger;
    }

    public INotificationProvider CreateProvider(string providerName)
    {
        if (!_config.Enabled)
            throw new InvalidOperationException("Notifications are disabled.");

        if (!_config.Providers.TryGetValue(providerName, out var providerConfig))
            throw new KeyNotFoundException($"Notification provider '{providerName}' is not configured.");

        _logger.LogInformation("Creating notification sender for {ProviderName} ({Type})", providerName, providerConfig.Type);

        return providerConfig switch
        {
            EmailConfiguration emailConfig => new EmailNotificationProvider(emailConfig, _logger),
            WebhookConfiguration emailConfig => new WebhookNotificationProvider(emailConfig, _logger),
            _ => throw new NotSupportedException($"Notification type '{providerConfig.Type}' not supported")
        };
    }
}