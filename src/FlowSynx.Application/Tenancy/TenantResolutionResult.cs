using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Networking;

namespace FlowSynx.Application.Tenancy;

public sealed record TenantResolutionResult
{
    public TenantId TenantId { get; init; }
    public TenantResolutionStatus ResolutionStatus { get; init; }
    public TenantStatus? TenantStatus { get; init; }
    public TenantCorsPolicy? CorsPolicy { get; init; }
    public TenantRateLimitingPolicy? RateLimitingPolicy { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}