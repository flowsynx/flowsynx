using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Application.Tenancy;

public interface ITenantContext
{
    TenantId TenantId { get; set; }
    bool IsValid { get; set; }
    AuthenticationMode AuthenticationMode { get; set; }
    TenantStatus Status { get; set; }
}
