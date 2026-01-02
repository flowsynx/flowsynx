using FlowSynx.Application.Tenancy;
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

        using (LogContext.PushProperty("TenantId", result.TenantId.ToString()))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        {
            logger.LogInformation("Tenant '{TenantId}' resolved successfully.", result.TenantId);
            await _next(context);
        }
    }
}