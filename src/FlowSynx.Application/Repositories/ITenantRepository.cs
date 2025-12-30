using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application;

public interface ITenantRepository
{
    Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken);

    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);

    Task AddAsync(Tenant entity, CancellationToken cancellationToken);

    Task UpdateAsync(Tenant entity, CancellationToken cancellationToken);

    Task DeleteAsync(TenantId id, CancellationToken cancellationToken);

    Task<TenantConfiguration> GetConfigurationAsync(
        TenantId tenantId, 
        CancellationToken cancellationToken = default);

    Task<T> GetConfigurationValueAsync<T>(
        TenantId tenantId, 
        string key, 
        T defaultValue = default, 
        CancellationToken cancellationToken = default);

    Task<ValidationResult> ValidateConfigurationAsync(
        TenantId tenantId, 
        CancellationToken cancellationToken = default);
}