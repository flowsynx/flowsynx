using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Application.Core.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public TenantId TenantId { get; set; }
    public bool IsValid { get; set; }
    public AuthenticationMode AuthenticationMode { get; set; }
    public TenantStatus Status { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IPAddress { get; set; }
    public string? Endpoint { get; set; }
}