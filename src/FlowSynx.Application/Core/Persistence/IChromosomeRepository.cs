using FlowSynx.Domain.Chromosomes;

namespace FlowSynx.Application.Core.Persistence;

public interface IChromosomeRepository
{
    Task<List<Chromosome>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Chromosome?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Chromosome?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default);
    Task<IEnumerable<Chromosome>> GetByGenomeIdAsync(Guid genomeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chromosome>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default);
    Task AddAsync(Chromosome entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}