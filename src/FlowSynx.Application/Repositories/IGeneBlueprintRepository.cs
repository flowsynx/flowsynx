using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application;

public interface IGeneBlueprintRepository
{
    Task<List<GeneBlueprint>> GetAllAsync(CancellationToken cancellationToken);
    Task<GeneBlueprint?> GetByIdAsync(GeneBlueprintId id, CancellationToken cancellationToken);
    Task<GeneBlueprint?> GetByGeneticBlueprintAsync(string geneticBlueprintId, CancellationToken cancellationToken);
    Task<List<GeneBlueprint>> SearchAsync(string searchTerm, CancellationToken cancellationToken);
    Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken);
    Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken);
    Task DeleteAsync(GeneBlueprintId id, CancellationToken cancellationToken);
    Task<bool> IsCompatibleAsync(GeneBlueprintId geneId, string runtimeVersion, string platform, CancellationToken cancellationToken);
}