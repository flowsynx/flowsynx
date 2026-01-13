using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithSecretsAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithConfigAsync(TenantId id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithContactAsync(TenantId id, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TenantId id, CancellationToken cancellationToken = default);
}