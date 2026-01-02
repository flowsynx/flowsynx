using FlowSynx.Application.Core.Interfaces;
using FlowSynx.Application.Core.Tenancy;
using FlowSynx.Domain.Tenants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace FlowSynx.Security;

public class DynamicAuthHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantRepository _tenantRepository;

    private readonly IEnumerable<IAuthenticationProvider> _authenticationProviders;

    public DynamicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ITenantContext tenantContext,
        IEnumerable<IAuthenticationProvider> authenticationProviders,
        ITenantRepository tenantRepository)
        : base(options, logger, encoder)
    {
        _tenantContext = tenantContext;
        _authenticationProviders = authenticationProviders;
        _tenantRepository = tenantRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId is null)
            return AuthenticateResult.Fail("Tenant not identified");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, CancellationToken.None);
        if (tenant is null || tenant.Status != TenantStatus.Active)
            return AuthenticateResult.Fail("Tenant not found or inactive");

        var provider = _authenticationProviders
            .FirstOrDefault(p =>
                p.AuthenticationMode == tenant.Configuration.Security.Authentication.Mode);

        if (provider is null)
            return AuthenticateResult.Fail("Unsupported authentication mode");

        var result = await provider.AuthenticateAsync(Context, tenant);

        if (!result.Succeeded)
            return AuthenticateResult.Fail(result.FailureReason!);

        return AuthenticateResult.Success(
            new AuthenticationTicket(result.Principal!, Scheme.Name));
    }
}