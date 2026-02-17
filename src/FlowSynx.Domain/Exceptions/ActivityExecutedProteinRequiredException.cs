using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ActivityExecutedProteinRequiredException : DomainException
{
    public ActivityExecutedProteinRequiredException()
        : base(
            DomainErrorCodes.ActivityExecutedProteinRequired,
            "Activity executed protein is required"
        )
    {
    }
}