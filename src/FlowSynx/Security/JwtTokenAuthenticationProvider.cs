using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public sealed class JwtTokenAuthenticationProvider : IAuthenticationProvider
{
    public TenantAuthenticationMode AuthenticationMode => TenantAuthenticationMode.Jwt;

    public Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        TenantId tenantId,
        TenantAuthenticationPolicy authenticationPolicy)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var value))
            return Task.FromResult(
                AuthenticationProviderResult.Fail("Missing Authorization header"));

        AuthenticationHeaderValue header;
        try
        {
            header = AuthenticationHeaderValue.Parse(value!);
        }
        catch
        {
            return Task.FromResult(
                AuthenticationProviderResult.Fail("Invalid Authorization header"));
        }

        if (!header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(
                AuthenticationProviderResult.Fail("Invalid authentication scheme"));

        var token = header.Parameter;
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(
                AuthenticationProviderResult.Fail("Missing bearer token"));

        // SignalR fallback (?access_token=)
        if (string.IsNullOrEmpty(token) &&
            context.Request.Query.TryGetValue("access_token", out var qsToken))
        {
            token = qsToken!;
        }

        var jwt = authenticationPolicy.Jwt;

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.Secret)),

            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),

            RequireSignedTokens = true,
            RequireExpirationTime = true,

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token!, validationParameters, out var validatedToken);

            // Enforce expected algorithm
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(
                    AuthenticationProviderResult.Fail("Invalid token algorithm"));
            }

            // Enforce authenticated identity
            if (!principal.Identity?.IsAuthenticated ?? true)
                return Task.FromResult(
                    AuthenticationProviderResult.Fail("Unauthenticated token"));

            // Normalize claims into system model
            var claims = new List<Claim>(principal.Claims)
            {
                new(CustomClaimTypes.TenantId, tenantId.ToString()),
                new(CustomClaimTypes.AuthMode, TenantAuthenticationMode.Jwt.ToString())
            };

            // Map JWT roles → permissions (only allow known permissions)
            var roleClaims = principal.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Where(Permissions.All.Contains)
                .Select(p => new Claim(CustomClaimTypes.Permissions, p));

            claims.AddRange(roleClaims);

            var identity = new ClaimsIdentity(claims, TenantAuthenticationMode.Jwt.ToString());
            var finalPrincipal = new ClaimsPrincipal(identity);

            context.User = finalPrincipal;

            return Task.FromResult(
                AuthenticationProviderResult.Success(finalPrincipal));
        }
        catch (SecurityTokenException ex)
        {
            return Task.FromResult(
                AuthenticationProviderResult.Fail($"Invalid token: {ex.Message}"));
        }
        catch (Exception)
        {
            return Task.FromResult(
                AuthenticationProviderResult.Fail("Token validation failed"));
        }
    }
}