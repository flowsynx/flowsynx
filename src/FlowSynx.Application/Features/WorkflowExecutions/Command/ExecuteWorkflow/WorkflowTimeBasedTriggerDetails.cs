namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTimeBasedTriggerDetails : Dictionary<string, object>
{
    public required string Cron { get; set; }
}