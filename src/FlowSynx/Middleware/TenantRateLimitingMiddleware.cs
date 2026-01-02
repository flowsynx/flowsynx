using FlowSynx.Application.Core.Tenancy;
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
        ITenantContext tenantContext,
        ITenantRateLimitPolicyProvider policyProvider)
    {
        var tenantId = tenantContext.TenantId;
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