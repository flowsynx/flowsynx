using FlowSynx.Domain.Entities;

namespace FlowSynx.Application.Services;

public interface ITenantService
{
    Guid? GetCurrentTenantId();
    Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<bool> SetCurrentTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
