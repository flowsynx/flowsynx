namespace FlowSynx.Application.Notifications;

public record NotificationMessage
{
    public string Title { get; init; } = string.Empty;  // Optional: useful for email, Teams, Slack
    public string Body { get; init; } = string.Empty;   // Main content
    //public List<string>? Recipients { get; set; } // Email, phone number, or webhook URL
    public Dictionary<string, string>? Metadata { get; init; } // Provider-specific or extra data
}

