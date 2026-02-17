using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ActivityTypeRequiredException : DomainException
{
    public ActivityTypeRequiredException()
        : base(
            DomainErrorCodes.ActivityTypeRequired,
            "Activity type is required"
        )
    {
    }
}