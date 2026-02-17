using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ActivityVersionRequiredException : DomainException
{
    public ActivityVersionRequiredException()
        : base(
            DomainErrorCodes.ActivityVersionRequired,
            "Activity version is required"
        )
    {
    }
}