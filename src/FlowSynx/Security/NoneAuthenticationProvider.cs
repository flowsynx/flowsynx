using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace FlowSynx.Security;

public sealed class NoneAuthenticationProvider : IAuthenticationProvider
{
    public AuthenticationMode AuthenticationMode => AuthenticationMode.None;

    public Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        AuthenticationScheme scheme)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("auth_mode", "none")
            };

        var identity = new ClaimsIdentity(claims, scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}