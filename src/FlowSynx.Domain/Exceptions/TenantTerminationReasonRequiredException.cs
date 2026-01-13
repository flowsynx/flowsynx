using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantTerminationReasonRequiredException : DomainException
{
    public TenantTerminationReasonRequiredException()
        : base(
            DomainErrorCodes.TenantTerminationReasonRequired,
            "Termination reason is required"
        )
    {
    }
}