using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Core.Tenancy;
using FlowSynx.Domain.Tenants;
using Serilog.Context;

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
        var result = await tenantResolver.ResolveAsync();
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

        // Valid and active tenant: populate context and push enrichment so downstream logging uses tenant-specific sinks.
        tenantContext.TenantId = result.TenantId;
        tenantContext.AuthenticationMode = result.AuthenticationMode;
        tenantContext.Status = result.Status;
        tenantContext.IsValid = result.IsValid;
        tenantContext.UserId = currentUserService.UserId();
        tenantContext.UserAgent = context.Request.Headers["User-Agent"].ToString();
        tenantContext.IPAddress = GetClientIpAddress(context);
        tenantContext.Endpoint = context.Request.Path;

        using (LogContext.PushProperty("TenantId", result.TenantId.ToString()))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        {
            logger.LogInformation("Tenant '{TenantId}' resolved successfully.", result.TenantId);
            await _next(context);
        }
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