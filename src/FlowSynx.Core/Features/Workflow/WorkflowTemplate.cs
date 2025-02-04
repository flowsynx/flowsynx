namespace FlowSynx.Core.Features.Workflow;

public class WorkflowTemplate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public WorkflowVariables Variables { get; set; } = new WorkflowVariables();
    public required WorkflowPipelines Pipelines { get; set; }
    public WorkflowOutputs? Outputs { get; set; }
}