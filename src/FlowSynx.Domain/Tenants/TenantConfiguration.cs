using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Domain.Tenants;

public sealed record TenantConfiguration
{
    public CorsConfiguration Cors { get; init; }
    public LocalizationConfiguration Localization { get; init; }
    public LoggingConfiguration Logging { get; init; }
    public RateLimitingConfiguration RateLimiting { get; init; }
    public SecretConfiguration Secret { get; init; }
    public SecurityConfiguration Security { get; init; }
}