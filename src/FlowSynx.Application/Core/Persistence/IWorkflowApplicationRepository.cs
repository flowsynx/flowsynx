using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;

namespace FlowSynx.Application.Core.Persistence;

public interface IWorkflowApplicationRepository
{
    Task<List<WorkflowApplication>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WorkflowApplication?> GetByIdAsync(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<WorkflowApplication?> GetByNameAsync(
        string name, 
        string @namespace = "default", 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowApplication>> GetByOwnerAsync(
        string owner, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowApplication>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId,
        string @namespace, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Workflow>> GetWorkflowsAsync(
        TenantId tenantId,
        string userId,
        Guid workflowApplicationId,
        CancellationToken cancellationToken = default);

    Task<bool> Exist(
        TenantId tenantId,
        string userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(WorkflowApplication entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowApplication entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default);
}