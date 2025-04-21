namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTask
{
    public WorkflowTask(string name)
    {
        Name = name;
        Parameters = new Dictionary<string, object?>();
        Dependencies = new List<string>();
    }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public object? Type { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
    public int? Timeout { get; set; }
    public List<string> Dependencies { get; set; }
}