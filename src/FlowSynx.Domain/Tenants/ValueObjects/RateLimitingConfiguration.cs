namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record RateLimitingConfiguration
{
    public int WindowSeconds { get; init; }
    public int PermitLimit { get; init; }
    public int QueueLimit { get; init; }

    public static RateLimitingConfiguration Create()
    {
        return new RateLimitingConfiguration
        {
            WindowSeconds = 60,
            PermitLimit = 100,
            QueueLimit = 10
        };
    }
}