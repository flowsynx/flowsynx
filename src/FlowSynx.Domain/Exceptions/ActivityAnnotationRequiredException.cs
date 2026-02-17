using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ActivityAnnotationRequiredException : DomainException
{
    public ActivityAnnotationRequiredException()
        : base(
            DomainErrorCodes.ActivityAnnotationRequired,
            "Activity annotation is required"
        )
    {
    }
}