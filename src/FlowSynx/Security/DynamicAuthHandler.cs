using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace FlowSynx.Security;

public class DynamicAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITenantContext _tenantContext;
    private readonly ISecretProviderFactory _secretProviderFactory;

    private readonly IEnumerable<IAuthenticationProvider> _authenticationProviders;

    public DynamicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITenantContext tenantContext,
        IEnumerable<IAuthenticationProvider> authenticationProviders,
        ISecretProviderFactory secretProviderFactory)
        : base(options, logger, encoder)
    {
        _tenantContext = tenantContext;
        _authenticationProviders = authenticationProviders;
        _secretProviderFactory = secretProviderFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
            return AuthenticateResult.Fail("Tenant not identified");

        var secretProvider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
        var secrets = await secretProvider.GetSecretsAsync();
        TenantAuthenticationPolicy parsedAuthenticationPolicy = ParseAuthenticationPolicy(secrets);

        //if (tenant is null || tenant.Status != TenantStatus.Active)
        //    return AuthenticateResult.Fail("Tenant not found or inactive");

        var authenticationProvider = _authenticationProviders
            .FirstOrDefault(p =>
                p.AuthenticationMode == parsedAuthenticationPolicy.Mode);

        if (authenticationProvider is null)
            return AuthenticateResult.Fail("Unsupported authentication mode");

        var result = await authenticationProvider.AuthenticateAsync(Context, tenantId, parsedAuthenticationPolicy);

        if (!result.Succeeded)
            return AuthenticateResult.Fail(result.FailureReason!);

        return AuthenticateResult.Success(
            new AuthenticationTicket(result.Principal!, Scheme.Name));
    }

    private TenantAuthenticationPolicy ParseAuthenticationPolicy(Dictionary<string, string?> secrets)
    {
        return new TenantAuthenticationPolicy
        {
            Mode = Enum.TryParse<TenantAuthenticationMode>(secrets.GetValueOrDefault("security:authentication:mode"), out var mode) ? mode : TenantAuthenticationMode.None,
            Basic = new TenantBasicPolicy
            {
                Users = new List<TenantBasicAuthenticationPolicy>
                {
                    new TenantBasicAuthenticationPolicy
                    {
                        Id = secrets.GetValueOrDefault("security:authentication:basic:id") ?? string.Empty,
                        UserName = secrets.GetValueOrDefault("security:authentication:basic:username") ?? string.Empty,
                        Password = secrets.GetValueOrDefault("security:authentication:basic:password") ?? string.Empty,
                        Roles = (secrets.GetValueOrDefault("security:authentication:basic:roles") ?? string.Empty)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList()
                    }
                }
            },
            Jwt = new TenantJwtAuthenticationPolicy
            {
                Issuer = secrets.GetValueOrDefault("security:authentication:jwt:issuer") ?? string.Empty,
                Audience = secrets.GetValueOrDefault("security:authentication:jwt:audience") ?? string.Empty,
                Authority = secrets.GetValueOrDefault("security:authentication:jwt:authority") ?? string.Empty,
                Name = secrets.GetValueOrDefault("security:authentication:jwt:name") ?? string.Empty,
                Secret = secrets.GetValueOrDefault("security:authentication:jwt:secret") ?? string.Empty,
                RequireHttps = bool.TryParse(secrets.GetValueOrDefault("security:authentication:jwt:requireHttps"), out var requireHttps) && requireHttps,
                RoleClaimNames = (secrets.GetValueOrDefault("security:authentication:jwt:roleClaimNames") ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList()
            }
        };
    }
}