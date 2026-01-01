using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Application.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public TenantId TenantId { get; set; }
    public bool IsValid { get; set; }
    public AuthenticationMode AuthenticationMode { get; set; }
    public TenantStatus Status { get; set; }
}