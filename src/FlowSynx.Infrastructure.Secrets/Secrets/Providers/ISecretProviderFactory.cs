using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;

namespace FlowSynx.Infrastructure.Security.Secrets.Providers;

public interface ISecretProviderFactory
{
    Task<ISecretProvider> GetProviderForTenantAsync(TenantId tenantId, CancellationToken ct = default);
    ISecretProvider CreateProvider(TenantId tenantId, SecretProviderType providerType, ProviderConfiguration configuration);
}