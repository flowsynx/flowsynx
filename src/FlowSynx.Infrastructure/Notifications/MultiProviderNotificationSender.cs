using FlowSynx.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Notifications;

public class MultiProviderNotificationSender : INotificationProvider
{
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger _logger;

    public MultiProviderNotificationSender(IEnumerable<INotificationProvider> providers, ILogger logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(provider =>
        {
            try
            {
                return provider.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification via {SenderType}", provider.GetType().Name);
                return Task.CompletedTask;
            }
        }).ToList();

        await Task.WhenAll(tasks);
    }
}
