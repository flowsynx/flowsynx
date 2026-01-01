using FlowSynx.Application;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public sealed class BasicAuthenticationProvider : IAuthenticationProvider
{
    private readonly ITenantService _tenantService;
    private readonly ITenantRepository _tenantRepository;

    public AuthenticationMode AuthenticationMode => AuthenticationMode.Basic;

    public BasicAuthenticationProvider(
        ITenantService tenantService,
        ITenantRepository tenantRepository)
    {
        _tenantService = tenantService;
        _tenantRepository = tenantRepository;
    }

    public async Task<AuthenticateResult> AuthenticateAsync(
        HttpContext context,
        AuthenticationScheme scheme)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail("Missing Authorization Header");
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers.Authorization!);
            if (authHeader.Scheme != "Basic")
                return AuthenticateResult.Fail("Invalid Authorization Scheme");

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return AuthenticateResult.Fail("Invalid Authorization Header");

            var username = credentials[0];
            var password = credentials[1];

            var tenantId = _tenantService.GetCurrentTenantId();
            var config = await _tenantRepository.GetByIdAsync(tenantId, CancellationToken.None);
            var securityConfiguration = config.Configuration.Security.Authentication;

            var users = securityConfiguration.Basic.Users;
            var user = users.FirstOrDefault(u => u.Name == username && u.Password == password);
            if (user == null)
                return AuthenticateResult.Fail("Invalid Username or Password");

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim("TenantId", tenantId.ToString())
                };
            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch
        {
            return AuthenticateResult.Fail("Invalid Authorization Header");
        }
    }
}