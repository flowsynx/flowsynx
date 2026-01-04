using FlowSynx.Application.Tenancy;
using FlowSynx.Infrastructure.Logging;
using Serilog;
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

        // Choose logger:
        // - Valid tenant: tenant-specific logger (console + file + seq per factory config)
        // - No/invalid tenant: global logger (console-only)
        var requestLogger = tenantId is not null
            ? tenantLoggerFactory.GetLogger(tenantId)
            : Log.Logger;

        // Enrich log context. Only include TenantId when available.
        using (tenantId is not null ? LogContext.PushProperty("TenantId", tenantId.ToString()) : default)
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
                if (tenantId is not null)
                {
                    requestLogger.Error(ex, "Request failed for tenant {TenantId}", tenantId);
                }
                else
                {
                    requestLogger.Error(ex, "Request failed");
                }
                throw;
            }
            finally
            {
                sw.Stop();

                if (tenantId is not null)
                {
                    requestLogger.Information(
                        "Request completed in {ElapsedMilliseconds}ms with status {StatusCode} for tenant {TenantId}",
                        sw.ElapsedMilliseconds,
                        context.Response.StatusCode,
                        tenantId);
                }
                else
                {
                    requestLogger.Information(
                        "Request completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                        sw.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
        }
    }
}