namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowTask
{
    public WorkflowTask(string name)
    {
        Name = name;
        Parameters = new WorkflowTaskParameter();
        Dependencies = new List<string>();
        Status = WorkflowTaskStatus.Pending;
    }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public object? Type { get; set; }
    public required string Process { get; set; }
    public List<string> Dependencies { get; set; }
    public WorkflowTaskParameter? Parameters { get; set; }
    public WorkflowTaskRetry? Retry { get; set; }
    public WorkflowTaskStatus Status { get; set; }
}