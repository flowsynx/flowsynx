namespace FlowSynx.Domain.Entities.Workflow;

public class WorkflowExecutionDefinition
{
    public WorkflowTasks WorkflowTasks { get; set; } = new();
    public WorkflowVariables WorkflowVariables { get; set; } = new();
    public WorkflowExecutionConfiguration Configuration { get; set; } = new();
}