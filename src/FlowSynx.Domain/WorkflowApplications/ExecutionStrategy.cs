using FlowSynx.Domain.Activities;

namespace FlowSynx.Domain.WorkflowApplications;

public class ExecutionStrategy
{
    public ErrorHandlingStrategy ErrorHandling { get; set; } = ErrorHandlingStrategy.Propagate;
    public int MaxParallelism { get; set; } = 3;
    public int TimeoutMilliseconds { get; set; } = 300000;
    public RetryPolicy RetryPolicy { get; set; } = new();
}