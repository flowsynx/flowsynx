using FlowSynx.Application.Services;

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
        ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();

        if (tenantId is null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid Tenant ID");
            return;
        }

        var isTenant = await tenantService.SetCurrentTenantAsync(tenantId);
        if (!isTenant)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Tenant not found or inactive");
            return;
        }

        // Set tenant context for this request
        var tenant = await tenantService.GetCurrentTenantAsync();
        context.Items["Tenant"] = tenant;

        await _next(context);
    }
}