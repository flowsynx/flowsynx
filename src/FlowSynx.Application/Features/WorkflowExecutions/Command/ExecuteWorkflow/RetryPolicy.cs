namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Fixed;
    public int InitialDelayMilliseconds { get; set; } = 1000;    // In millisecond
    public int MaxDelayMilliseconds { get; set; } = 10000;      // In millisecond
    public double BackoffCoefficient { get; set; } = 2.0;
}