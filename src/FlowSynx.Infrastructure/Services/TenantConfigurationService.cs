//using FlowSynx.Application.Abstractions.Services;
//using FlowSynx.Domain.Tenants;
//using FlowSynx.Domain.TenantSecretConfigs.Logging;
//using FlowSynx.Domain.TenantSecretConfigs.Networking;
//using FlowSynx.Domain.TenantSecretConfigs.Security;
//using Microsoft.Extensions.Logging;

//namespace FlowSynx.Infrastructure.Abstractions.Services;

//public class TenantConfigurationService : ITenantConfigurationService
//{
//    private readonly ISecretProvider _secretProvider;
//    private readonly ILogger<TenantConfigurationService> _logger;

//    public TenantConfigurationService(
//        ISecretProvider secretProvider,
//        ILogger<TenantConfigurationService> logger)
//    {
//        _secretProvider = secretProvider;
//        _logger = logger;
//    }

//    public async Task<TenantAuthenticationPolicy?> GetAuthenticationConfigAsync(TenantId tenantId, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public async Task<TenantCorsPolicy?> GetCorsConfigAsync(TenantId tenantId, CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            var secrets = await _secretProvider.GetSecretsAsync(tenantId, "cors.", cancellationToken);

//            if (!secrets.TryGetValue("auth.jwtSecret", out var jwtSecret) || string.IsNullOrEmpty(jwtSecret))
//                return null;

//            return new TenantCorsPolicy
//            {
//                PolicyName = secrets.GetValueOrDefault("cors.policyName"),
//                AllowedOrigins = secrets.GetValueOrDefault("cors.allowedOrigins")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
//                AllowCredentials = bool.TryParse(secrets.GetValueOrDefault("cors.allowCredentials"), out var allowCredentials) && allowCredentials
//            };
//        }
//        catch (Exception ex)
//        {
//            _logger.LogWarning(ex, "Failed to get auth config for tenant {TenantId}", tenantId);
//            return null;
//        }
//    }

//    public Task<TenantLoggingPolicy?> GetLoggingConfigAsync(TenantId tenantId, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<TenantRateLimitingPolicy?> GetRateLimitConfigAsync(TenantId tenantId, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }
//}