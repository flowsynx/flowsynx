using FlowSynx.Application;
using FlowSynx.Application.Services;

namespace FlowSynx.Middleware;

public class TenantCorsMiddleware
{
    private readonly RequestDelegate _next;

    public TenantCorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId != null)
        {
            var tenantRepository = context.RequestServices.GetRequiredService<ITenantRepository>();

            // Get tenant-specific CORS settings
            var origins = await tenantRepository.GetConfigurationValueAsync<string[]>(
                tenantId,
                "Cors:AllowedOrigins", ["*"]);
            var allowCredentials = await tenantRepository.GetConfigurationValueAsync<bool>(
                tenantId,
                "Cors:AllowCredentials", false);

            // Apply CORS headers based on tenant config
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if (origins.Contains("*") || (origin != null && origins.Contains(origin)))
            {
                if (origin != null)
                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;

                if (allowCredentials)
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }
        }

        await _next(context);
    }
}