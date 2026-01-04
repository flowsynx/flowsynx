using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Networking;
using FlowSynx.Domain.TenantSecretConfigs.Security;

namespace FlowSynx.Application.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public TenantId TenantId { get; set; }
    public bool IsValid { get; set; }
    //public TenantAuthenticationMode AuthenticationMode { get; set; }
    public TenantStatus Status { get; set; }
    public TenantCorsPolicy? CorsPolicy { get; set; }
    public TenantRateLimitingPolicy? RateLimitingPolicy { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IPAddress { get; set; }
    public string? Endpoint { get; set; }
}