namespace FlowSynx.Domain.Activities;

public class FaultHandling
{
    public ErrorHandlingStrategy ErrorHandling { get; set; } = ErrorHandlingStrategy.Propagate;
    public RetryPolicy? RetryPolicy { get; set; }
    public Fallback? Fallback { get; set; }
    public CircuitBreaker? CircuitBreaker { get; set; }
    public HealthCheck? HealthCheck { get; set; }
}