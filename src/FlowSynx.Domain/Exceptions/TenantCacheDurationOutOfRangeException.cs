using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantCacheDurationOutOfRangeException : DomainException
{
    public TenantCacheDurationOutOfRangeException()
        : base(
            DomainErrorCodes.TenantCacheDurationOutOfRange,
            "Cache duration must be between 1 and 1440 minutes"
        )
    {
    }
}