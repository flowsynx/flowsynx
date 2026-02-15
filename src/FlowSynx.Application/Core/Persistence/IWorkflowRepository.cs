using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Application.Core.Persistence;

public interface IWorkflowRepository
{
    Task<List<Workflow>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default);

    Task<Workflow?> GetByIdAsync(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<Workflow?> GetByNameAsync(
        string name, 
        string @namespace = "default", 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Workflow>> GetByGenomeIdAsync(
        Guid genomeId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Workflow>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Domain.ActivityInstances.ActivityInstance>> GetWorkflowActivitiesAsync(
        TenantId tenantId,
        string userId,
        Guid workflowId,
        CancellationToken cancellationToken = default);

    Task<bool> Exist(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);

    Task AddAsync(Workflow entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Workflow entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);
}