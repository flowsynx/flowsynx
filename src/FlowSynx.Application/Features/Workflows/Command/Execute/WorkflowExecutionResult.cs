namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowExecutionResult
{
    public Guid WorkflowExecutionId { get; set; }
    public required string Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<TaskExecutionResult> TaskResults { get; set; } = new();
}

public class TaskExecutionResult
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string Status { get; set; }
    public string? Output { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}