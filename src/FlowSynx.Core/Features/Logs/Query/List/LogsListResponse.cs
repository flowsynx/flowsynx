using FlowSynx.Domain.Entities.Log;

namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogsLevel Level { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}