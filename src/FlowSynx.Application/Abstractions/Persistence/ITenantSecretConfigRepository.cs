using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;

namespace FlowSynx.Application.Abstractions.Persistence;

public interface ITenantSecretConfigRepository
{
    Task<TenantSecretConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantSecretConfig?> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantSecretConfig>> GetEnabledConfigsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}