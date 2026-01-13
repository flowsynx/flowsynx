namespace FlowSynx.Domain.TenantSecretConfigs.Networking;

public sealed record TenantCorsPolicy
{
    public string? PolicyName { get; init; }
    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
    public bool AllowCredentials { get; init; } = false;

    public static TenantCorsPolicy Create()
    {
        return new TenantCorsPolicy
        {
            PolicyName = "DefaultCorsPolicy",
            AllowedOrigins = Array.Empty<string>(),
            AllowCredentials = false
        };
    }
}