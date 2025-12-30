namespace FlowSynx.HealthCheck;

public class HealthCheckResponse
{
    public string? Status { get; set; }
    public TimeSpan HealthCheckDuration { get; set; }
}