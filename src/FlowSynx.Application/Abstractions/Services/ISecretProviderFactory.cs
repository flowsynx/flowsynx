using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;

namespace FlowSynx.Application.Abstractions.Services;

public interface ISecretProviderFactory
{
    Task<ISecretProvider> GetProviderForTenantAsync(TenantId tenantId, CancellationToken ct = default);
    ISecretProvider CreateProvider(TenantId tenantId, SecretProviderType providerType, ProviderConfiguration configuration);
}