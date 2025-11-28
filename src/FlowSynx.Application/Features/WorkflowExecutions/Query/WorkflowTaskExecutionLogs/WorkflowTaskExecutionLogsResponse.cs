namespace FlowSynx.Application.Features.WorkflowExecutions.Query.WorkflowTaskExecutionLogs;

public class WorkflowTaskExecutionLogsResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}