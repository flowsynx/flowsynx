using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public class LogMessage
{
    public string? UserId { get; set; }
    public required string Message { get; set; }
    public required LogLevel Level { get; set; }
    public EventId EventId { get; set; }
    public required DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;
    public string? Exception { get; set; }
}