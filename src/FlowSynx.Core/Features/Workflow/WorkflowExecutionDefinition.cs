namespace FlowSynx.Core.Features.Workflow;

public class WorkflowExecutionDefinition
{
    public WorkflowPipelines WorkflowPipelines { get; set; } = new();
    public WorkflowVariables WorkflowVariables { get; set; } = new();
    public WorkflowExecutionConfiguration Configuration { get; set; } = new();
}