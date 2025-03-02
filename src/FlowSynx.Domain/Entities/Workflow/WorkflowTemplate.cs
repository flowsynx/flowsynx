namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowTemplate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowVariables Variables { get; set; } = new WorkflowVariables();
    public required WorkflowTasks Tasks { get; set; }
    public WorkflowOutputs? Outputs { get; set; }
}