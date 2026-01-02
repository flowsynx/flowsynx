using System.Security.Claims;

namespace FlowSynx.Security;

public sealed class AuthenticationProviderResult
{
    public bool Succeeded { get; }
    public ClaimsPrincipal? Principal { get; }
    public string? FailureReason { get; }

    private AuthenticationProviderResult(
        bool succeeded,
        ClaimsPrincipal? principal,
        string? failureReason)
    {
        Succeeded = succeeded;
        Principal = principal;
        FailureReason = failureReason;
    }

    public static AuthenticationProviderResult Success(ClaimsPrincipal principal)
        => new(true, principal, null);

    public static AuthenticationProviderResult Fail(string reason)
        => new(false, null, reason);
}