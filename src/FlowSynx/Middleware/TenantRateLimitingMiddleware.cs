using FlowSynx.Application.Services;
using FlowSynx.Services;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Middleware;

public sealed class TenantRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public TenantRateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantService tenantService,
        ITenantRateLimitPolicyProvider policyProvider)
    {
        var tenantId = tenantService.GetCurrentTenantId();
        if (!Guid.TryParse(tenantId?.ToString(), out var parsedTenantId))
        {
            await _next(context);
            return;
        }

        var policy = await policyProvider.GetPolicyAsync(tenantId, context.RequestAborted);
        if (policy is null)
        {
            await _next(context);
            return;
        }

        var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var cacheKey = $"tenant:ratelimit:{tenantId}:{clientKey}";

        var counter = _cache.GetOrCreate(
            cacheKey,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(policy.WindowSeconds);
                return new Lazy<RateLimitCounter>(
                    () => new RateLimitCounter(),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                });

        var current = Interlocked.Increment(ref counter!.Value.Count);
        if (current > policy.PermitLimit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }

        await _next(context);
    }
}

internal sealed class RateLimitCounter
{
    public int Count;
}





//public class TenantRateLimitingMiddleware
//{
//    private readonly RequestDelegate _next;
//    private readonly IMemoryCache _cache;

//    public TenantRateLimitingMiddleware(
//        RequestDelegate next,
//        IMemoryCache cache)
//    {
//        _next = next;
//        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
//    }

//    public async Task InvokeAsync(
//        HttpContext context, 
//        ITenantService tenantService,
//        ITenantRepository tenantRepository)
//    {
//        var tenantId = tenantService.GetCurrentTenantId();
//        if (tenantId == null || !Guid.TryParse(tenantId.ToString(), out var parsedTenantId))
//        {
//            await _next(context);
//            return;
//        }

//        // Get tenant-specific rate limits
//        var window = await tenantRepository.GetConfigurationValueAsync<int>(
//            tenantId,
//            "RateLimiting:WindowSeconds",
//            60);

//        var limit = await tenantRepository.GetConfigurationValueAsync<int>(
//            tenantId,
//            "RateLimiting:PermitLimit",
//            100);

//        var queueLimit = await tenantRepository.GetConfigurationValueAsync<int>(
//            tenantId,
//            "RateLimiting:QueueLimit",
//            10);

//        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//        var cacheKey = $"RateLimit_{tenantId}_{remoteIp}";

//        var requestCount = _cache.GetOrCreate(cacheKey, entry =>
//        {
//            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(window);
//            return 0;
//        });

//        if (requestCount >= limit)
//        {
//            context.Response.StatusCode = 429;
//            await context.Response.WriteAsync("Rate limit exceeded");
//            return;
//        }

//        _cache.Set(cacheKey, requestCount + 1);
//        await _next(context);
//    }
//}