namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class FlowControlConfig
{
    public List<string> Dependencies { get; set; } = new();
    public List<string> RunOnFailureOf { get; set; } = new();
    public Condition? ExecutionCondition { get; set; }
    public List<ConditionalBranch> ConditionalBranches { get; set; } = new();
}