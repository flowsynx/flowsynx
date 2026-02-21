namespace FlowSynx.Domain.Workflows;

public class WorkflowSpecification
{
    public string Description { get; set; } = string.Empty;
    public List<ActivityInstance> Activities { get; set; } = new List<ActivityInstance>();

    public ExecutionContext Context { get; set; } = new ExecutionContext();

    public WorkflowValidation Validation { get; set; } = new();

    public WorkflowOutput Output { get; set; } = new();
}