namespace FlowSynx.Application.Features.Workflows.Command.Execute;

public class WorkflowTimeBasedTriggerDetails: Dictionary<string, object>
{
    public required string Cron { get; set; }
}