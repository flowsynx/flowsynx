namespace FlowSynx.Domain.Entities.Logs;

public class Log
{
    public Guid Id { get; set; }
    public string? UserId { get; set; } = string.Empty;
    public required string Message { get; set; }
    public required LogsLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public required DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public string? Exception { get; set; }
}