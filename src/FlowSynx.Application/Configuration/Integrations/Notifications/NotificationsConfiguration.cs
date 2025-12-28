using FlowSynx.Domain.Primitives;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Configuration.Integrations.Notifications;

public class NotificationsConfiguration
{
    public bool Enabled { get; set; } = false;
    public string? BaseUrl { get; set; }
    public List<string> DefaultProviders { get; set; } = new();
    public Dictionary<string, NotificationProviderConfiguration> Providers { get; set; } = new();

    public void ValidateNotificationProviders(ILogger logger)
    {
        if (!Enabled)
            return;

        var validProviders = Providers.Keys.ToList();
        logger.LogInformation("Configured Notification Providers: {Providers}", string.Join(", ", validProviders));

        var invalidProviders = DefaultProviders
            .Where(provider => !validProviders.Contains(provider))
            .ToList();

        if (invalidProviders.Any())
        {
            throw new FlowSynxException(
                (int)ErrorCode.NotificationConfigurationInvalidProviderName,
                $"Invalid default providers '{string.Join(", ", invalidProviders)}'. Available: {string.Join(", ", validProviders)}"
            );
        }

        if (!DefaultProviders.Any())
            logger.LogWarning("No default notification providers defined.");
    }
}