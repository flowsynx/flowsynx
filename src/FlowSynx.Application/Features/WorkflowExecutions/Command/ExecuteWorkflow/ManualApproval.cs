namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ManualApproval
{
    public bool Enabled { get; set; } = false;
    public string Comment { get; set; } = string.Empty;
}