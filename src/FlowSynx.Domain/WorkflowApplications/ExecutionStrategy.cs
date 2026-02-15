using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.WorkflowApplications;

public class ExecutionStrategy
{
    public string Mode { get; set; } = "sequential"; // sequential, parallel, dependency

    public int MaxParallelism { get; set; } = 3;

    public int TimeoutMilliseconds { get; set; } = 300000;

    public RetryPolicy RetryPolicy { get; set; } = new();
}