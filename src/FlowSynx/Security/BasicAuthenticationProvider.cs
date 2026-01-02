using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public sealed class BasicAuthenticationProvider : IAuthenticationProvider
{
    public AuthenticationMode AuthenticationMode => AuthenticationMode.Basic;

    public async Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        Tenant tenant)
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

        var securityConfig = tenant.Configuration.Security.Authentication;
        var users = securityConfig.Basic.Users;

        var user = users.FirstOrDefault(u =>
            u.Name.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            u.Password == password);

        if (user is null)
            return AuthenticationProviderResult.Fail("Invalid username or password");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(CustomClaimTypes.TenantId, tenant.Id.ToString()),
            new(CustomClaimTypes.AuthMode, AuthenticationMode.Basic.ToString())
        };

        claims.AddRange(user.Roles
            .Select(r => new Claim(CustomClaimTypes.Permissions, r)));

        var identity = new ClaimsIdentity(claims, "Basic");
        var principal = new ClaimsPrincipal(identity);

        return AuthenticationProviderResult.Success(principal);
    }
}