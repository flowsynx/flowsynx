using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using FlowSynx.Application.Configuration.Core.Security;

namespace FlowSynx.Security;

public class BasicAuthenticationProvider : IAuthenticationProvider
{
    public string SchemeName => "Basic";
    
    public void Configure(AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(SchemeName, null);
    }

    public class BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SecurityConfiguration securityConfiguration)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }

            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers.Authorization!);
                if (authHeader.Scheme != "Basic")
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));

                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                if (credentials.Length != 2)
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

                var username = credentials[0];
                var password = credentials[1];

                var users = securityConfiguration.Authentication.BasicUsers;
                var user = users.FirstOrDefault(u => u.Name == username && u.Password == password);
                if (user == null)
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name)
                };
                claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }
        }
    }
}