namespace FlowSynx.Domain.Tenants.ValueObjects;

public sealed record CorsConfiguration
{
    public string? PolicyName { get; init; }
    public List<string> AllowedOrigins { get; init; } = new();
    public bool AllowCredentials { get; init; } = false;

    public static CorsConfiguration Create()
    {
        return new CorsConfiguration
        {
            PolicyName = "DefaultCorsPolicy",
            AllowedOrigins = new List<string>(),
            AllowCredentials = false
        };
    }
}