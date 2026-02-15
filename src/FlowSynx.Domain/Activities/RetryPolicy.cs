namespace FlowSynx.Domain.Activities;

public class RetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 1000;
    public float BackoffMultiplier { get; set; } = 1.5f;
    public int MaxDelayMilliseconds { get; set; } = 10000;
}