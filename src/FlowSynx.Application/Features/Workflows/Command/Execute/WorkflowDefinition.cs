namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowDefinition
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowConfiguration Configuration { get; set; } = new();
    public WorkflowVariables Variables { get; set; } = new WorkflowVariables();
    public required WorkflowTasks Tasks { get; set; } = new WorkflowTasks();
    public WorkflowOutputs? Outputs { get; set; } = new WorkflowOutputs();
}