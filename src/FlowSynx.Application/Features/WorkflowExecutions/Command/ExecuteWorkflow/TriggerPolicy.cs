namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class TriggerPolicy
{
    public required string TaskName { get; set; }
    public bool SkipCurrentTaskAfterTrigger { get; set; } = true;
}