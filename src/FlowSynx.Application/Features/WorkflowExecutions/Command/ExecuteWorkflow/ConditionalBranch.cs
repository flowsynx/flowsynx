namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ConditionalBranch
{
    public required string Expression { get; set; }
    public string? Description { get; set; }
    public required string TargetTaskName { get; set; }
}