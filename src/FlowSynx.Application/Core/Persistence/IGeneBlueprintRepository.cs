using FlowSynx.Domain.GeneBlueprints;

namespace FlowSynx.Application.Core.Persistence;

public interface IGeneBlueprintRepository
{
    Task<List<GeneBlueprint>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<GeneBlueprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GeneBlueprint?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneBlueprint>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default);
    Task<List<GeneBlueprint>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    //Task<bool> IsEpistaticCompatibleAsync(GeneBlueprintId geneId, string runtimeVersion, string platform, CancellationToken cancellationToken);
}