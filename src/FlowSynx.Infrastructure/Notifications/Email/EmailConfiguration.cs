using FlowSynx.Application.Configuration.Integrations.Notifications;

namespace FlowSynx.Infrastructure.Notifications.Email;

public class EmailConfiguration: NotificationProviderConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public EmailCredentials Credentials { get; set; } = new EmailCredentials();
    public EmailSender Sender { get; set; } = new();
    public List<string>? Recipients { get; set; } = new();
}