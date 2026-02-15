namespace FlowSynx.Application.Models;

public class ExecutionError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public bool Recoverable { get; set; } = false;
}