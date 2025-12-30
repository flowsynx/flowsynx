using FlowSynx.Application.Services;
using FlowSynx.Infrastructure.Configuration.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    // If the request is for the SignalR hub
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs/workflowExecutions"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var tenantService = context.HttpContext.RequestServices.GetRequiredService<ITenantService>();
                    var tenantId = context.Principal?.FindFirst("tenantId")?.Value;

                    if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out var tenantGuid))
                    {
                        await tenantService.SetCurrentTenantAsync(tenantGuid);
                    }
                }
            };
        });

        builder.Services.AddScoped<IClaimsTransformation>(sp => new RoleClaimsTransformation(_config));
    }
}