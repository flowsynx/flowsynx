using FlowSynx.Application;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Services;

public sealed class TenantRateLimitPolicyProvider : ITenantRateLimitPolicyProvider
{
    private readonly IMemoryCache _cache;
    private readonly ITenantRepository _repository;

    private static readonly RateLimitingConfiguration DefaultPolicy = new()
    {
        WindowSeconds = 60,
        QueueLimit = 10,
        PermitLimit = 100
    };

    public TenantRateLimitPolicyProvider(
        IMemoryCache cache,
        ITenantRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async ValueTask<RateLimitingConfiguration?> GetPolicyAsync(TenantId tenantId, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync(
            $"tenant:ratelimit:policy:{tenantId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var config = await _repository.GetByIdAsync(tenantId, ct);
                var windowSeconds = config.Configuration.RateLimiting.WindowSeconds;
                var permitLimit = config.Configuration.RateLimiting.PermitLimit;

                return new RateLimitingConfiguration
                {
                    WindowSeconds = windowSeconds,
                    PermitLimit = permitLimit
                };
            });
    }
}
