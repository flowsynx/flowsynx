using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.AspNetCore.Authentication;

namespace FlowSynx.Security;

public interface IAuthenticationProvider
{
    AuthenticationMode AuthenticationMode { get; }

    Task<AuthenticationProviderResult> AuthenticateAsync(
        HttpContext context,
        Tenant tenant);
}