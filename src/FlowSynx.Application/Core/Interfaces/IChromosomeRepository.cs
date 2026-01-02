using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Core.Interfaces;

public interface IChromosomeRepository
{
    Task<List<Chromosome>> GetAllAsync(CancellationToken cancellationToken);
    Task<Chromosome?> GetByIdAsync(ChromosomeId id, CancellationToken cancellationToken);
    Task<List<Chromosome>> GetByGenomeAsync(GenomeId genomeId, CancellationToken cancellationToken);
    Task AddAsync(Chromosome entity, CancellationToken cancellationToken);
    Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken);
    Task DeleteAsync(ChromosomeId id, CancellationToken cancellationToken);
}