using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Application.Core.Tenancy;

public interface ITenantContext
{
    TenantId TenantId { get; set; }
    bool IsValid { get; set; }
    AuthenticationMode AuthenticationMode { get; set; }
    TenantStatus Status { get; set; }
    string UserId { get; set; }
    string? UserAgent { get; set; }
    string? IPAddress { get; set; }
    string? Endpoint { get; set; }
}
