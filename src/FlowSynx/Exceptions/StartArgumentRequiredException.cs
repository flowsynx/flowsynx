using FlowSynx.Errors;

namespace FlowSynx.Exceptions;

public sealed class StartArgumentRequiredException : SystemException
{
    public StartArgumentRequiredException()
        : base(
            SystemErrorCodes.StartArgumentIsRequired,
            "The '--start' argument is required."
        )
    {
    }
}