using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Workflow;

public class WorkflowTask
{
    public WorkflowTask(string name)
    {
        Name = name;
        Options = new ConnectorOptions();
        Dependencies = new List<string>();
        Status = WorkflowTaskStatus.Pending;
    }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public object? Type { get; set; }
    public required string Process { get; set; }
    public List<string> Dependencies { get; set; }
    public ConnectorOptions? Options { get; set; }
    public WorkflowTaskStatus Status { get; set; }
}