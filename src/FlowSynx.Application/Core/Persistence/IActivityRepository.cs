using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Persistence;

public interface IActivityRepository
{
    Task<List<Activity>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<Activity?> GetByIdAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<Activity?> GetByNameAndVersionAsync(
        string name, 
        string version, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Activity>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default);

    Task<List<Activity>> SearchAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);

    Task AddAsync(Activity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Activity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);
}