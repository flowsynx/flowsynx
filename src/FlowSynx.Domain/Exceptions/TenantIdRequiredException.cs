using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantIdRequiredException : DomainException
{
    public TenantIdRequiredException()
        : base(
            DomainErrorCodes.TenantIdRequired,
            $"Tenant ID is required"
        )
    {
    }
}