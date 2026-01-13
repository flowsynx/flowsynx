using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantEmailRequiredException : DomainException
{
    public TenantEmailRequiredException()
        : base(
            DomainErrorCodes.TenantEmailRequired,
            $"Tenant email is required"
        )
    {
    }
}