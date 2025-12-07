namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class ExecutionConfig
{
    public string Operation { get; set; } = string.Empty;
    public Dictionary<string, object?> Specification { get; set; } = new();
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public AgentConfiguration? Agent { get; set; }
    public int? TimeoutMilliseconds { get; set; }
}