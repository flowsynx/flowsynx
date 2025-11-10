namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class Condition
{
    public required string Expression { get; set; }
    public string? Description { get; set; }
}
