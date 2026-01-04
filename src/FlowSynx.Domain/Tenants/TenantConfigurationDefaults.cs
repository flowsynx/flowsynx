//using FlowSynx.Domain.Tenants.ValueObjects;
//using FlowSynx.Domain.TenantSecretConfigs.Localization;
//using FlowSynx.Domain.TenantSecretConfigs.Logging;
//using FlowSynx.Domain.TenantSecretConfigs.Networking;

//namespace FlowSynx.Domain.Tenants;

//public static class TenantConfigurationDefaults
//{
//    public static TenantConfiguration Create()
//        => new TenantConfiguration
//        {
//            Cors = CorsConfiguration.Create(),
//            Localization = LocalizationConfiguration.Create(),
//            Logging = LoggingConfiguration.Create(),
//            RateLimiting = RateLimitingConfiguration.Create(),
//            Secret = SecretConfiguration.Create(),
//            Security = SecurityConfiguration.Create()
//        };
//}