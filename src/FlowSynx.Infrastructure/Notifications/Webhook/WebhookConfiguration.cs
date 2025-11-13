using FlowSynx.Application.Configuration.Integrations.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Webhook;

public class WebhookConfiguration : NotificationProviderConfiguration
{
    public string WebhookUrl { get; set; } = string.Empty;
    public Dictionary<string, string>? Headers { get; set; }
}