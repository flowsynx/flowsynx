using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Configuration.System.RateLimiting;

namespace FlowSynx.Services;

public interface ITenantRateLimitPolicyProvider
{
    ValueTask<RateLimitingConfiguration?> GetPolicyAsync(TenantId tenantId, CancellationToken ct);
}