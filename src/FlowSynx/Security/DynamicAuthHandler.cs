using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.TenantSecretConfigs.Security;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
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
        TenantAuthenticationPolicy parsedAuthenticationPolicy = secrets.GetAuthenticationPolicy();

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
}