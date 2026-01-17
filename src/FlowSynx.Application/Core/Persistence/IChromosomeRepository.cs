using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface IChromosomeRepository
{
    Task<List<Chromosome>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<Chromosome?> GetByIdAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);

    Task<Chromosome?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default);

    Task<IEnumerable<Chromosome>> GetByGenomeIdAsync(Guid genomeId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Chromosome>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default);

    Task AddAsync(Chromosome entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);
}