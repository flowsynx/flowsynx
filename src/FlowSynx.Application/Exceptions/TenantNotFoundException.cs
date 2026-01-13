using FlowSynx.Application.Errors;

namespace FlowSynx.Application.Exceptions;

public sealed class TenantNotFoundException : ApplicationException
{
    public TenantNotFoundException(Guid tenantId)
        : base(
            ApplicationErrorCodes.TenantNotFound,
            $"The tenant with id '{tenantId}' is not found."
        )
    {
    }
}