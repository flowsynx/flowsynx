namespace FlowSynx.Domain.TenantSecretConfigs.Networking;

public sealed record TenantRateLimitingPolicy
{
    public int WindowSeconds { get; init; }
    public int PermitLimit { get; init; }
    public int QueueLimit { get; init; }

    public static TenantRateLimitingPolicy Create()
    {
        return new TenantRateLimitingPolicy
        {
            WindowSeconds = 60,
            PermitLimit = 100,
            QueueLimit = 10
        };
    }
}