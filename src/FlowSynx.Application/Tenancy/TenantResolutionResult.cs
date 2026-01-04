using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Networking;

namespace FlowSynx.Application.Tenancy;

public sealed record TenantResolutionResult
{
    public TenantId TenantId { get; init; }
    public bool IsValid { get; set; }
    //public TenantAuthenticationMode AuthenticationMode { get; init; }
    public TenantStatus Status { get; init; }
    public TenantCorsPolicy? CorsPolicy { get; init; }
    public TenantRateLimitingPolicy? RateLimitingPolicy { get; init; }
}