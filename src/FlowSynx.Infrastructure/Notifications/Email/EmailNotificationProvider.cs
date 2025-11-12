using FlowSynx.Application.Notifications;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace FlowSynx.Infrastructure.Notifications.Email;

public class EmailNotificationProvider : INotificationProvider
{
    private readonly EmailConfiguration _config;
    private readonly ILogger _logger;

    public EmailNotificationProvider(EmailConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var toList = _config.Recipients?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList() ?? new List<string>();

        if (toList.Count == 0)
        {
            _logger.LogWarning("No recipients provided for email with subject '{Subject}'.", message.Title);
            return;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_config.Sender.Address, _config.Sender.Name),
            Subject = message.Title,
            Body = message.Body,
            IsBodyHtml = true
        };

        foreach (var to in toList)
            mailMessage.To.Add(to);

        using var client = new SmtpClient(_config.Host, _config.Port)
        {
            EnableSsl = true
        };

        if (!string.IsNullOrEmpty(_config.Credentials.UserName))
        {
            client.Credentials = new NetworkCredential(_config.Credentials.UserName, _config.Credentials.Password);
        }

        await Task.Run(() => client.Send(mailMessage), cancellationToken);
        _logger.LogInformation("Email sent to {Count} recipient(s) with subject '{Subject}'.", toList.Count, message.Title);
    }
}
