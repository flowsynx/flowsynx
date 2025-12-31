using FlowSynx.Application;
using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Configuration.System.RateLimiting;
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

                var windowSeconds = await _repository.GetConfigurationValueAsync<int>(
                    tenantId, "RateLimiting:WindowSeconds", 60);

                var permitLimit = await _repository.GetConfigurationValueAsync<int>(
                    tenantId, "RateLimiting:PermitLimit", 100);

                return new RateLimitingConfiguration
                {
                    WindowSeconds = windowSeconds,
                    PermitLimit = permitLimit
                };
            });
    }
}
