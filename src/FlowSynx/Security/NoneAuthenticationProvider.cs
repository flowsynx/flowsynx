using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using System.Security.Claims;

namespace FlowSynx.Security;

public sealed class NoneAuthenticationProvider : IAuthenticationProvider
{
    public AuthenticationMode AuthenticationMode => AuthenticationMode.None;

    public Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        Tenant tenant)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new(ClaimTypes.Name, "admin"),
            new(CustomClaimTypes.TenantId, tenant.Id.ToString()),
            new(CustomClaimTypes.AuthMode, AuthenticationMode.None.ToString()),
            new(CustomClaimTypes.Permissions, Permissions.Admin)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "None");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationProviderResult.Success(principal));
    }
}