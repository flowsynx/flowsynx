using FlowSynx.Application.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace FlowSynx.Security;

public class JwtAuthenticationProvider : IAuthenticationProvider
{
    private readonly JwtAuthenticationsConfiguration _config;
    public string SchemeName => _config.Name;

    public JwtAuthenticationProvider(JwtAuthenticationsConfiguration config)
    {
        _config = config;
    }

    public void Configure(AuthenticationBuilder builder)
    {
        builder.AddJwtBearer(SchemeName, options =>
        {
            options.Authority = _config.Authority;
            options.Audience = _config.Audience;
            options.RequireHttpsMetadata = _config.RequireHttps;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = _config.Issuer,
                ValidAudience = _config.Audience,
                RoleClaimType = ClaimTypes.Role,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret))
            };
        });

        builder.Services.AddScoped<IClaimsTransformation>(sp => new RoleClaimsTransformation(_config));
    }
}