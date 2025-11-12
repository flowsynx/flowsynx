using FlowSynx.Application.Configuration.Integrations.Notifications;
using FlowSynx.Application.Notifications;
using FlowSynx.Infrastructure.Notifications.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Notifications;

public class NotificationProviderFactory: INotificationProviderFactory
{
    private readonly NotificationsConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationProviderFactory> _logger;

    public NotificationProviderFactory(
        NotificationsConfiguration config, 
        IServiceProvider serviceProvider,
        ILogger<NotificationProviderFactory> logger)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private INotificationProvider CreateSingleProvider(string providerName)
    {
        if (!_config.Enabled)
            throw new InvalidOperationException("Notifications are disabled.");

        if (!_config.Providers.TryGetValue(providerName, out var providerConfig))
            throw new KeyNotFoundException($"Notification provider '{providerName}' is not configured.");

        _logger.LogInformation("Creating notification sender for {ProviderName} ({Type})", providerName, providerConfig.Type);

        return providerConfig.Type.ToLower() switch
        {
            "smtp" => ActivateSender<EmailNotificationProvider>(providerConfig, providerName),
            _ => throw new NotSupportedException($"Notification type '{providerConfig.Type}' not supported")
        };
    }

    private T ActivateSender<T>(NotificationProviderConfiguration config, string providerName)
        where T : class, INotificationProvider
    {
        // Resolve from DI with parameter injection
        return ActivatorUtilities.CreateInstance<T>(_serviceProvider, config, _logger);
    }

    public INotificationProvider Create(IEnumerable<string>? providerNames = null)
    {
        providerNames ??= _config.DefaultProviders;

        var providers = providerNames
            .Where(p => _config.Providers.ContainsKey(p))
            .Select(CreateSingleProvider)
            .ToList();

        return new MultiProviderNotificationSender(providers, _logger);
    }
}
