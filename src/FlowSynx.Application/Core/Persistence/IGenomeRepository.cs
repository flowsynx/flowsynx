using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application.Core.Persistence;

public interface IGenomeRepository
{
    Task<List<Genome>> GetAllAsync(CancellationToken cancellationToken);
    Task<Genome?> GetByIdAsync(GenomeId id, CancellationToken cancellationToken);
    Task AddAsync(Genome entity, CancellationToken cancellationToken);
    Task UpdateAsync(Genome entity, CancellationToken cancellationToken);
    Task DeleteAsync(GenomeId id, CancellationToken cancellationToken);
    Task<List<Genome>> GetByMetadataAsync(string key, object value, CancellationToken cancellationToken);
}