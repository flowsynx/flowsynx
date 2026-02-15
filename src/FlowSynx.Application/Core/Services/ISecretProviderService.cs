using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Application.Core.Services;

public interface ISecretProviderService
{
    Task<string?> GetSecretAsync(
        TenantId tenantId, 
        SecretKey secretKey, 
        CancellationToken ct = default);

    Task<Dictionary<string, string?>> GetSecretsAsync(
        TenantId tenantId, 
        string? prefix = null, 
        CancellationToken ct = default);

    Task<bool> ValidateConnectionAsync(
        TenantId tenantId, 
        CancellationToken ct = default);

    Task SetSecretAsync(
        TenantId tenantId, 
        SecretKey secretKey, 
        SecretValue secretValue, 
        CancellationToken ct = default);
}