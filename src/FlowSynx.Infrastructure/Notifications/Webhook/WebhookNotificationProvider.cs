using FlowSynx.Application.Configuration.Integrations.Notifications;
using FlowSynx.Application.Notifications;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Notifications.Webhook;

public class WebhookNotificationProvider : INotificationProvider
{
    private readonly WebhookConfiguration _config;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public WebhookNotificationProvider(NotificationProviderConfiguration config, ILogger logger)
    {
        if (config is not WebhookConfiguration webhookConfig)
            throw new ArgumentException("Invalid configuration type. Expected WebhookConfiguration.", nameof(config));

        _config = webhookConfig;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WebhookUrl))
        {
            _logger.LogWarning("Webhook URL is not configured for message '{Title}'.", message.Title);
            return;
        }

        // Generic payload
        var payload = new
        {
            title = message.Title,
            body = message.Body
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Add optional headers
        if (_config.Headers != null)
        {
            foreach (var header in _config.Headers)
            {
                if (!content.Headers.Contains(header.Key))
                    content.Headers.Add(header.Key, header.Value);
            }
        }

        try
        {
            var response = await _httpClient.PostAsync(_config.WebhookUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send webhook message '{Title}'. StatusCode: {StatusCode}", message.Title, response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Webhook message sent successfully: '{Title}'", message.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending webhook message '{Title}'", message.Title);
        }
    }
}