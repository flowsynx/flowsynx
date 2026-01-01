using FlowSynx.Application.Tenancy;
using FlowSynx.Infrastructure.Logging;
using Serilog.Context;

namespace FlowSynx.Middleware;

public class TenantLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ITenantContext tenantContext, 
        ITenantLoggerFactory tenantLoggerFactory)
    {
        var tenantId = tenantContext.TenantId;

        if (tenantId == null)
        {
            await _next(context);
            return;
        }

        // Create tenant-specific logger
        var tenantLogger = tenantLoggerFactory.GetLogger(tenantId);

        using (LogContext.PushProperty("TenantId", tenantId.ToString()))
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