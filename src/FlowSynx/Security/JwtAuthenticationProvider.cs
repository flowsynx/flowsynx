using FlowSynx.Application;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public sealed class JwtAuthenticationProvider : IAuthenticationProvider
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRepository _tenantRepository;

    public AuthenticationMode AuthenticationMode => AuthenticationMode.Jwt;

    public JwtAuthenticationProvider(
        ITenantContext tenantContext,
        ITenantRepository tenantRepository)
    {
        _tenantContext = tenantContext;
        _tenantRepository = tenantRepository;
    }

    public async Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        AuthenticationScheme scheme)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header))
            return AuthenticateResult.Fail("Missing Authorization header");

        if (!header.ToString().StartsWith("Bearer "))
            return AuthenticateResult.Fail("Invalid authentication scheme");

        var token = header.ToString()["Bearer ".Length..];

        var tenantId = _tenantContext.TenantId;
        var config = await _tenantRepository.GetByIdAsync(tenantId, CancellationToken.None);
        var securityConfiguration = config.Configuration.Security.Authentication;

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = securityConfiguration.Jwt.Issuer,
            ValidAudience = securityConfiguration.Jwt.Audience,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityConfiguration.Jwt.Secret))
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);
            context.User = principal;

            return AuthenticateResult.Success(new AuthenticationTicket(principal, scheme.Name));
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid JWT token");
        }
    }
}