namespace FlowSynx.Domain.Activities;

public class CircuitBreaker
{
    public int FailureThreshold { get; set; } = 5;
    public int SuccessThreshold { get; set; } = 2;
    public int TimeoutMilliseconds { get; set; } = 30000;
    public int HalfOpenMaxCalls { get; set; } = 3;
}