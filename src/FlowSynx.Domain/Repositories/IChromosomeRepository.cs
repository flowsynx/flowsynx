using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Repositories;

public interface IChromosomeRepository
{
    Task<List<Chromosome>> GetByGenomeAsync(GenomeId genomeId);
}