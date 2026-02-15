namespace FlowSynx.Domain.WorkflowApplications;

public class WorkflowApplicationSpecification
{
    public string Description { get; set; } = string.Empty;

    public List<WorkflowReference> Workflows { get; set; } = new();

    public ApplicationEnvironment Environment { get; set; } = new();

    public ApplicationValidation Validation { get; set; } = new();

    public ExecutionStrategy Execution { get; set; } = new();

    public ApplicationOutput Output { get; set; } = new();
}