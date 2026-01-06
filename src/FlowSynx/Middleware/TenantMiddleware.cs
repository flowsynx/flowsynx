using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Logging;

namespace FlowSynx.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserService currentUserService,
        ITenantResolver tenantResolver,
        ITenantContext tenantContext,
        ILogger<TenantMiddleware> logger)
    {
        var result = await tenantResolver.ResolveAsync(context.RequestAborted);
        if (result is null || !result.IsValid)
        {
            logger.LogError("Tenant resolution failed. Invalid Tenant ID.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid Tenant ID");
            return;
        }

        if (result.Status != TenantStatus.Active)
        {
            logger.LogWarning("Tenant '{TenantId}' is not active. Status: {Status}", result.TenantId, result.Status);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Tenant is not active.");
            return;
        }

        TenantContextAccessor.Set(new TenantContextAccessor.TenantContext
        {
            TenantId = result.TenantId,
            IsValid = result.IsValid,
            Status = result.Status,
            CorsPolicy = result.CorsPolicy,
            RateLimitingPolicy = result.RateLimitingPolicy,
            UserId = currentUserService.UserId(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            IPAddress = GetClientIpAddress(context),
            Endpoint = context.Request.Path
        });

        var loggingService = context.RequestServices.GetRequiredService<TenantLoggingService>();
        await loggingService.ConfigureTenantLogger(result.TenantId);

        await _next(context);
    }

    private string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // Try to get IP from various headers
        var headers = new[] { "X-Forwarded-For", "X-Real-IP" };
        foreach (var header in headers)
        {
            if (httpContext.Request.Headers.TryGetValue(header, out var value))
            {
                var ip = value.ToString().Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}