namespace FlowSynx.Application.Features.Workflows.Command.ExecuteWorkflow;

public class WorkflowTimeBasedTriggerDetails: Dictionary<string, object>
{
    public required string Cron { get; set; }
}