//using FlowSynx.Domain.Tenants;
//using FlowSynx.Domain.TenantSecretConfigs.Logging;
//using FlowSynx.Domain.TenantSecretConfigs.Networking;
//using FlowSynx.Domain.TenantSecretConfigs.Security;

//namespace FlowSynx.Application.Abstractions.Services;

//public interface ITenantConfigurationService
//{
//    Task<TenantAuthenticationPolicy?> GetAuthenticationConfigAsync(
//        TenantId tenantId, 
//        CancellationToken cancellationToken = default);

//    Task<TenantCorsPolicy?> GetCorsConfigAsync(
//        TenantId tenantId, 
//        CancellationToken cancellationToken = default);

//    Task<TenantRateLimitingPolicy?> GetRateLimitConfigAsync(
//        TenantId tenantId, 
//        CancellationToken cancellationToken = default);

//    Task<TenantLoggingPolicy?> GetLoggingConfigAsync(
//        TenantId tenantId,
//        CancellationToken cancellationToken = default);
//}