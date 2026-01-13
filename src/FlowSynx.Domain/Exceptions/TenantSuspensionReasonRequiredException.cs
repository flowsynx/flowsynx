using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantSuspensionReasonRequiredException : DomainException
{
    public TenantSuspensionReasonRequiredException()
        : base(
            DomainErrorCodes.TenantSuspensionReasonRequired,
            "Suspension reason is required"
        )
    {
    }
}