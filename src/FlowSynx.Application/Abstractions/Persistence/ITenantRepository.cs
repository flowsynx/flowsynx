using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Abstractions.Persistence;

public interface ITenantRepository
{
    Task<Tenant?> GetWithSecretsAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithConfigAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithContactAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TenantId id, CancellationToken cancellationToken = default);
}