using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Interfaces;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);

    Task AddAsync(Tenant entity, CancellationToken cancellationToken);

    Task UpdateAsync(Tenant entity, CancellationToken cancellationToken);

    Task DeleteAsync(TenantId id, CancellationToken cancellationToken);
}