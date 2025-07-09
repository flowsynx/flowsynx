namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTask(string name)
{
    public required string Name { get; set; } = name;
    public string? Description { get; set; }
    public object? Type { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; } = new();
    public ErrorHandling? ErrorHandling { get; set; }
    public int? Timeout { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? Output { get; set; } = string.Empty;
}