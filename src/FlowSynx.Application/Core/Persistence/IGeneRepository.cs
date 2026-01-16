using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface IGeneRepository
{
    Task<List<Gene>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<Gene?> GetByIdAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<Gene?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    Task<IEnumerable<Gene>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default);

    Task<List<Gene>> SearchAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);

    Task AddAsync(Gene entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Gene entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);
}