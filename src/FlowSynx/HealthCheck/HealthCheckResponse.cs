namespace FlowSynx.HealthCheck;

public class HealthCheckResponse
{
    public string? Status { get; set; }
    public IEnumerable<IndividualHealthCheckResponse> HealthChecks { get; set; } = new List<IndividualHealthCheckResponse>();
    public TimeSpan HealthCheckDuration { get; set; }
}