using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public sealed class BasicAuthenticationProvider : IAuthenticationProvider
{
    public TenantAuthenticationMode AuthenticationMode => TenantAuthenticationMode.Basic;

    public async Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        TenantId tenantId,
        TenantAuthenticationPolicy authenticationPolicy)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header))
            return AuthenticationProviderResult.Fail("Missing Authorization header");

        AuthenticationHeaderValue authHeader;
        try
        {
            authHeader = AuthenticationHeaderValue.Parse(header!);
        }
        catch
        {
            return AuthenticationProviderResult.Fail("Invalid Authorization header");
        }

        if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            return AuthenticationProviderResult.Fail("Invalid Authorization scheme");

        if (string.IsNullOrWhiteSpace(authHeader.Parameter))
            return AuthenticationProviderResult.Fail("Missing credentials");

        string username;
        string password;

        try
        {
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes)
                .Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

            if (credentials.Length != 2)
                return AuthenticationProviderResult.Fail("Invalid credential format");

            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
            return AuthenticationProviderResult.Fail("Invalid Base64 credentials");
        }

        var users = authenticationPolicy.Basic.Users;

        var user = users.FirstOrDefault(u =>
            u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);

        if (user is null)
            return AuthenticationProviderResult.Fail("Invalid username or password");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(CustomClaimTypes.TenantId, tenantId.ToString()),
            new(CustomClaimTypes.AuthMode, TenantAuthenticationMode.Basic.ToString())
        };

        claims.AddRange(user.Roles
            .Select(r => new Claim(CustomClaimTypes.Permissions, r)));

        var identity = new ClaimsIdentity(claims, "Basic");
        var principal = new ClaimsPrincipal(identity);

        return AuthenticationProviderResult.Success(principal);
    }
}