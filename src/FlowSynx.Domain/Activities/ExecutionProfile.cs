namespace FlowSynx.Domain.Activities;

public class ExecutionProfile
{
    public string DefaultOperation { get; set; } = string.Empty;
    public List<ExecutionCondition> Conditions { get; set; } = new List<ExecutionCondition>();
    public int Priority { get; set; } = 1;
    public string ExecutionMode { get; set; } = "synchronous";
    public int TimeoutMilliseconds { get; set; } = 5000;
    public RetryPolicy RetryPolicy { get; set; } = new RetryPolicy();
}