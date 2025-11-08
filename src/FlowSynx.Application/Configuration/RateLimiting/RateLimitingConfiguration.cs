namespace FlowSynx.Application.Configuration.RateLimiting;

public class RateLimitingConfiguration
{
    public int WindowSeconds { get; set; } = 60;
    public int PermitLimit { get; set; } = 100;
    public int QueueLimit { get; set; } = 10;
}