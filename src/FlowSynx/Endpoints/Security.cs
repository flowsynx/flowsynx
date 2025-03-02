//using FlowSynx.Core.Configuration;
//using FlowSynx.Core.Extensions;
//using FlowSynx.Core.Features.PluginConfig.Query.List;
//using FlowSynx.Core.Services;
//using FlowSynx.Extensions;
//using MediatR;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.FileSystemGlobbing.Internal;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using System.CommandLine;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//namespace FlowSynx.Endpoints;

//public class Security : EndpointGroupBase
//{
//    public override void Map(WebApplication app)
//    {
//        var group = app.MapGroup(this);

//        group.MapPost("/login", Login)
//            .WithName("Login")
//            .WithOpenApi();
//    }

//    public IResult Login(ClaimsPrincipal user, IHttpClientFactory httpClientFactory,
//        IOptions<SecurityConfiguration> securityConfiguration, CancellationToken cancellationToken)
//    {
//        if (securityConfiguration.Value.OAuth2.Enabled)
//            return Results.BadRequest("Login is managed by OAuth2/OpenID. Use OAuth2 login flow.");

//        return Results.Ok($"Hello, {user.Identity?.Name}! You are authenticated."));
//    }

//    private string GenerateJwtToken(string username, string key)
//    {
//        var claims = new[]
//        {
//        new Claim(ClaimTypes.Name, username)
//    };

//        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
//        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//        var jwtToken = new JwtSecurityToken(
//            claims: claims,
//            expires: DateTime.UtcNow.AddHours(1),
//            signingCredentials: creds
//        );

//        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
//    }
//}