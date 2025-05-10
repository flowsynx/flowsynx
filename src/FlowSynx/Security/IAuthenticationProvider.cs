using Microsoft.AspNetCore.Authentication;

namespace FlowSynx.Security;

public interface IAuthenticationProvider
{
    string SchemeName { get; }
    void Configure(AuthenticationBuilder builder);
}