namespace FlowSynx.Application.Models;

public class ExecutionResponseMetadata
{
    public string Id { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long Duration { get; set; }
}