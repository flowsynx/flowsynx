using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Tenancy;

namespace FlowSynx.Middleware;

public sealed class TenantCorsMiddleware
{
    private readonly RequestDelegate _next;

    public TenantCorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext)
    {
        var tenantId = tenantContext.TenantId;
        if (tenantId == null)
        {
            await _next(context);
            return;
        }

        var corsConfig = tenantContext.CorsPolicy;
        var origins = corsConfig?.AllowedOrigins;
        var allowCredentials = corsConfig?.AllowCredentials ?? false;

        var origin = context.Request.Headers.Origin.ToString();
        if (origins.Contains("*") || (origin != null && origins.Contains(origin)))
        {
            if (origin != null)
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;

            if (allowCredentials)
                context.Response.Headers["Access-Control-Allow-Credentials"] = "true";

            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
        }

        // Handle preflight
        if (context.Request.Method == HttpMethods.Options)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await _next(context);
    }
}





//public class TenantCorsMiddleware
//{
//    private readonly RequestDelegate _next;

//    public TenantCorsMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
//    {
//        var tenantId = tenantService.GetCurrentTenantId();
//        if (tenantId != null)
//        {
//            var tenantRepository = context.RequestServices.GetRequiredService<ITenantRepository>();

//            // Get tenant-specific CORS settings
//            var origins = await tenantRepository.GetConfigurationValueAsync<string[]>(
//                tenantId,
//                "Cors:AllowedOrigins", ["*"]);
//            var allowCredentials = await tenantRepository.GetConfigurationValueAsync<bool>(
//                tenantId,
//                "Cors:AllowCredentials", false);

//            // Apply CORS headers based on tenant config
//            var origin = context.Request.Headers["Origin"].FirstOrDefault();
//            if (origins.Contains("*") || (origin != null && origins.Contains(origin)))
//            {
//                if (origin != null)
//                    context.Response.Headers["Access-Control-Allow-Origin"] = origin;

//                if (allowCredentials)
//                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
//            }
//        }

//        await _next(context);
//    }
//}