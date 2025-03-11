namespace FlowSynx.Domain.Entities.Workflow.Models;

public class WorkflowDefinition
{
    public WorkflowExecutionConfiguration Configuration { get; set; } = new();
    public WorkflowVariables WorkflowVariables { get; set; } = new();
    public WorkflowTasks WorkflowTasks { get; set; } = new();
}