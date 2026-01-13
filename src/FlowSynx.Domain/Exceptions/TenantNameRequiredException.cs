using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantNameRequiredException : DomainException
{
    public TenantNameRequiredException()
        : base(
            DomainErrorCodes.TenantNameRequired,
            $"Tenant name is required"
        )
    {
    }
}