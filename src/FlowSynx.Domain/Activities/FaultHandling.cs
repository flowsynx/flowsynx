namespace FlowSynx.Domain.Activities;

public class FaultHandling
{
    public string ErrorHandling { get; set; } = "propagate";
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 1000;
    public Fallback Fallback { get; set; } = new Fallback();
    public CircuitBreaker CircuitBreaker { get; set; } = new CircuitBreaker();
    public HealthCheck HealthCheck { get; set; } = new HealthCheck();
}