using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.AspNetCore.Authentication;

namespace FlowSynx.Security;

public interface IAuthenticationProvider
{
    AuthenticationMode AuthenticationMode { get; }

    Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        AuthenticationScheme scheme);
}