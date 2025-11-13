using FlowSynx.Application.Configuration.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FlowSynx.Security;

public class DisabledAuthenticationProvider : IAuthenticationProvider
{
    public string SchemeName => "Disabled";

    public void Configure(AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, DisabledAuthenticationHandler>(SchemeName, null);
    }

    private sealed class DisabledAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SecurityConfiguration _securityConfiguration;

        public DisabledAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            SecurityConfiguration securityConfiguration)
            : base(options, logger, encoder)
        {
            _securityConfiguration = securityConfiguration ?? throw new ArgumentNullException(nameof(securityConfiguration));
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_securityConfiguration.Authentication.Enabled)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Always impersonate a fixed "admin" user in disabled mode.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "admin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}