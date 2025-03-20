namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowDefinition
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public WorkflowConfiguration Configuration { get; set; } = new();
    public required List<WorkflowTask> Tasks { get; set; } = new List<WorkflowTask>();
}