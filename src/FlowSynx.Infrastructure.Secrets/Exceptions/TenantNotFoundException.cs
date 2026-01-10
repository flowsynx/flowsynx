using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Security.Errors;

namespace FlowSynx.Infrastructure.Security.Exceptions;

public sealed class TenantNotFoundException : SecurityException
{
    public TenantNotFoundException(TenantId tenantId)
        : base(
            SecurityErrorCodes.TenantNotFound,
            $"Tenant {tenantId} not found"
        )
    {
    }
}