using FlowSynx.Application;
using FlowSynx.Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Middleware;

public class TenantRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public TenantRateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache)
    {
        _next = next;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (tenantId == null || !Guid.TryParse(tenantId.ToString(), out var parsedTenantId))
        {
            await _next(context);
            return;
        }

        var tenantRepository = context.RequestServices.GetRequiredService<ITenantRepository>();

        // Get tenant-specific rate limits
        var window = await tenantRepository.GetConfigurationValueAsync<int>(
            tenantId,
            "RateLimiting:WindowSeconds",
            60);

        var limit = await tenantRepository.GetConfigurationValueAsync<int>(
            tenantId,
            "RateLimiting:PermitLimit",
            100);

        var queueLimit = await tenantRepository.GetConfigurationValueAsync<int>(
            tenantId,
            "RateLimiting:QueueLimit",
            10);

        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"RateLimit_{tenantId}_{remoteIp}";

        var requestCount = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(window);
            return 0;
        });

        if (requestCount >= limit)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        _cache.Set(cacheKey, requestCount + 1);
        await _next(context);
    }
}