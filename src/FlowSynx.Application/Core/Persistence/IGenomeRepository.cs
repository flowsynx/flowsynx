using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface IGenomeRepository
{
    Task<List<Genome>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Genome?> GetByIdAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);
    Task<Genome?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default);
    Task<IEnumerable<Genome>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default);
    Task<IEnumerable<Genome>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default);
    Task AddAsync(Genome entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Genome entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);
}