using FlowSynx.Infrastructure.Runtime.Errors;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public sealed class ActivityNotFoundException : RuntimeException
{
    public ActivityNotFoundException(Guid activityId)
        : base(
            RuntimeErrorCodes.ActivityNotFound,
            $"Activity not found: {activityId}"
        )
    {
    }
}