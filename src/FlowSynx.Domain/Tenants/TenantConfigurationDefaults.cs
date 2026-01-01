using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Domain.Tenants;

public static class TenantConfigurationDefaults
{
    public static TenantConfiguration Create()
        => new TenantConfiguration
        {
            Cors = CorsConfiguration.Create(),
            Localization = LocalizationConfiguration.Create(),
            Logging = LoggerConfiguration.Create(),
            RateLimiting = RateLimitingConfiguration.Create(),
            Secret = SecretConfiguration.Create(),
            Security = SecurityConfiguration.Create()
        };
}