using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Services;

public interface ITenantRateLimitPolicyProvider
{
    ValueTask<RateLimitingConfiguration?> GetPolicyAsync(TenantId tenantId, CancellationToken ct);
}