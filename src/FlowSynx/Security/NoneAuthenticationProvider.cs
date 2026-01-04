using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using System.Security.Claims;

namespace FlowSynx.Security;

public sealed class NoneAuthenticationProvider : IAuthenticationProvider
{
    public TenantAuthenticationMode AuthenticationMode => TenantAuthenticationMode.None;

    public Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        TenantId tenantId,
        TenantAuthenticationPolicy authenticationPolicy)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
            new(ClaimTypes.Name, "admin"),
            new(CustomClaimTypes.TenantId, tenantId.ToString()),
            new(CustomClaimTypes.AuthMode, TenantAuthenticationMode.None.ToString()),
            new(CustomClaimTypes.Permissions, Permissions.Admin)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "None");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationProviderResult.Success(principal));
    }
}