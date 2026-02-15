namespace FlowSynx.Application.Models;

public class ExecutionLog
{
    public string Level { get; set; } = "info"; // "info", "warn", "error", "debug"
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}