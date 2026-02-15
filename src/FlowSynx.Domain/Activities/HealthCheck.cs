namespace FlowSynx.Domain.Activities;

public class HealthCheck
{
    public string Endpoint { get; set; } = string.Empty;
    public int IntervalMilliseconds { get; set; } = 30000;
    public int TimeoutMilliseconds { get; set; } = 5000;
}