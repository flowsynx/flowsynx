using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Logging;

namespace FlowSynx.Middleware;

public sealed class TenantMiddleware
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
        TenantResolutionResult result;

        try
        {
            result = await tenantResolver.ResolveAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tenant resolution threw an exception.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Tenant resolution failed.");
            return;
        }

        if (result is null)
        {
            logger.LogError("Tenant resolution returned null.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        // 1️⃣ Resolution-level validation
        switch (result.ResolutionStatus)
        {
            case TenantResolutionStatus.Invalid:
                logger.LogWarning("Invalid tenant identifier. Messages: {Messages}", result.Messages);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid tenant identifier.");
                return;

            case TenantResolutionStatus.NotFound:
                logger.LogWarning("Tenant not found. Messages: {Messages}", result.Messages);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Tenant not found.");
                return;

            case TenantResolutionStatus.Forbidden:
                logger.LogWarning("Tenant access forbidden. Messages: {Messages}", result.Messages);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;

            case TenantResolutionStatus.Error:
                logger.LogError("Tenant resolution error. Messages: {Messages}", result.Messages);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;

            case TenantResolutionStatus.Active:
                break;

            default:
                logger.LogError("Unhandled tenant resolution status: {Status}", result.ResolutionStatus);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
        }

        // 2️⃣ Lifecycle-level authorization
        if (result.TenantStatus != TenantStatus.Active)
        {
            logger.LogWarning(
                "Tenant '{TenantId}' is not active. Status: {Status}.",
                result.TenantId,
                result.TenantStatus);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Tenant is not active.");
            return;
        }

        // 3️⃣ Populate tenant context
        TenantContextAccessor.Set(new TenantContextAccessor.TenantContext
        {
            TenantId = result.TenantId!,
            Status = result.TenantStatus!.Value,
            CorsPolicy = result.CorsPolicy,
            RateLimitingPolicy = result.RateLimitingPolicy,
            UserId = currentUserService.UserId(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            IPAddress = GetClientIpAddress(context),
            Endpoint = context.Request.Path
        });

        // 4️⃣ Configure tenant-aware logging
        var loggingService = context.RequestServices.GetRequiredService<TenantLoggingService>();
        await loggingService.ConfigureTenantLoggerAsync(result.TenantId!);

        await _next(context);
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var headers = new[] { "X-Forwarded-For", "X-Real-IP" };

        foreach (var header in headers)
        {
            if (context.Request.Headers.TryGetValue(header, out var value))
            {
                var ip = value.ToString().Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
