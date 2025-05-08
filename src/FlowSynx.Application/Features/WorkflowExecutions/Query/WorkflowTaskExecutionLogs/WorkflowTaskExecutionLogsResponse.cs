using FlowSynx.Domain.Log;

namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

public class WorkflowTaskExecutionLogsResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public LogsLevel Level { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}