using FlowSynx.Domain.Aggregates;

namespace FlowSynx.Domain.Repositories;

public interface IGenomeRepository
{
    Task<List<Genome>> GetByMetadataAsync(string key, object value);
}