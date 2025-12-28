using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Repositories;

public interface IGeneBlueprintRepository
{
    Task<GeneBlueprint> GetByGeneticBlueprintAsync(string geneticBlueprintId);
    Task<List<GeneBlueprint>> SearchAsync(string searchTerm);
    Task<bool> IsCompatibleAsync(GeneBlueprintId geneId, string runtimeVersion, string platform);
}