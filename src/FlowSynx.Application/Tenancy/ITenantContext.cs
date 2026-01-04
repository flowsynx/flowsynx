using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Networking;
using FlowSynx.Domain.TenantSecretConfigs.Security;

namespace FlowSynx.Application.Tenancy;

public interface ITenantContext
{
    TenantId TenantId { get; set; }
    bool IsValid { get; set; }
    //TenantAuthenticationMode AuthenticationMode { get; set; }
    TenantStatus Status { get; set; }
    TenantCorsPolicy? CorsPolicy { get; set; }
    TenantRateLimitingPolicy? RateLimitingPolicy { get; set; }
    string UserId { get; set; }
    string? UserAgent { get; set; }
    string? IPAddress { get; set; }
    string? Endpoint { get; set; }
}
