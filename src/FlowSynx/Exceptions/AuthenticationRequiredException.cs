using FlowSynx.Errors;

namespace FlowSynx.Exceptions;

public sealed class AuthenticationRequiredException : SystemException
{
    public AuthenticationRequiredException()
        : base(
            SystemErrorCodes.AuthenticationRequired,
            "Access is denied. Please check and ensure you are using valid credentials."
        )
    {
    }
}