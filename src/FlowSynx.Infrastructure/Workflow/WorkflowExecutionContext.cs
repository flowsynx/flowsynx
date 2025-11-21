namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowExecutionContext
{
    public string UserId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public Dictionary<string, object?>? WorkflowVariables { get; set; } = new();
    public Dictionary<string, object>? TaskOutputs { get; set; } = new();
}