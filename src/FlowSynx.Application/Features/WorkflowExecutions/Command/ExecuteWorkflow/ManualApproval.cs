namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ManualApproval
{
    public bool Enabled { get; set; } = false;
    public List<string> Approvers { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
    public string DefaultAction { get; set; } = "abort";
}