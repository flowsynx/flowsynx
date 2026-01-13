using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantContactEmailRequiredException : DomainException
{
    public TenantContactEmailRequiredException()
        : base(
            DomainErrorCodes.TenantContactEmailRequired,
            "Contact email cannot be empty"
        )
    {
    }
}