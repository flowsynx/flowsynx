using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Security;

namespace FlowSynx.Security;

public interface IAuthenticationProvider
{
    TenantAuthenticationMode AuthenticationMode { get; }

    Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        TenantId tenantId,
        TenantAuthenticationPolicy authenticationPolicy);
}