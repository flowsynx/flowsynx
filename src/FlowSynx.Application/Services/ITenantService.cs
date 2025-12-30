using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Services;

public interface ITenantService
{
    TenantId? GetCurrentTenantId();
    Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<bool> SetCurrentTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}
