using FlowSynx.Application.Services;
using FlowSynx.Extensions;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog.Context;

namespace FlowSynx.Middleware;

public class TenantLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();

        if (tenantId == null)
        {
            await _next(context);
            return;
        }

        // Make TenantId available to loggers without resolving ITenantService
        context.Items["TenantId"] = tenantId;
        await tenantService.SetCurrentTenantAsync(tenantId);

        // Create tenant-specific logger
        var tenantLogger = TenantLogging.CreateTenantLogger(tenantId.ToString());
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();

        // Add tenant logger to context
        context.Items["TenantLogger"] = tenantLogger;

        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("User", context.User.Identity?.Name ?? "anonymous"))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                tenantLogger.Error(ex, "Request failed for tenant {TenantId}", tenantId);
                throw;
            }
            finally
            {
                sw.Stop();
                tenantLogger.Information(
                    "Request completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                    sw.ElapsedMilliseconds,
                    context.Response.StatusCode);
            }
        }
    }
}