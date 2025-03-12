using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using FlowSynx.Application.Configuration;
using System.Net.Http.Headers;

namespace FlowSynx.Middleware;

// Basic Authentication Handler
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SecurityConfiguration _securityConfiguration;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SecurityConfiguration securityConfiguration)
        : base(options, logger, encoder)
    {
        _securityConfiguration = securityConfiguration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers.Authorization);
            if (authHeader.Scheme != "Basic")
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            if (credentials.Length != 2)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

            var username = credentials[0];
            var password = credentials[1];

            var users = _securityConfiguration.Basic.Users;
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

    //protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    //{
    //    if (!Request.Headers.ContainsKey("Authorization"))
    //        return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

    //    var authHeader = Request.Headers["Authorization"].ToString();
    //    if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    //        return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

    //    var token = authHeader.Substring("Basic ".Length).Trim();
    //    var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(token)).Split(':');

    //    if (credentials.Length != 2)
    //        return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

    //    var username = credentials[0];
    //    var password = credentials[1];

    //    var users = _securityConfiguration.Value.Users;
    //    if (!users.TryGetValue(username, out var validPassword) || validPassword != password)
    //        return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

    //    var claims = new[] { new Claim(ClaimTypes.Name, username) };
    //    var identity = new ClaimsIdentity(claims, Scheme.Name);
    //    var principal = new ClaimsPrincipal(identity);
    //    var ticket = new AuthenticationTicket(principal, Scheme.Name);

    //    return Task.FromResult(AuthenticateResult.Success(ticket));
    //}
}