using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface IGeneBlueprintRepository
{
    Task<List<GeneBlueprint>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<GeneBlueprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GeneBlueprint?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneBlueprint>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default);

    Task<List<GeneBlueprint>> SearchAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);

    Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}