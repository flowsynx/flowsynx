using FlowSynx.Infrastructure.Runtime.Errors;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public sealed class ActivityExecutorNotFoundException : RuntimeException
{
    public ActivityExecutorNotFoundException(string executorType)
        : base(
            RuntimeErrorCodes.ActivityExecutorNotFound,
            $"No executor found for activity type: {executorType}"
        )
    {
    }
}