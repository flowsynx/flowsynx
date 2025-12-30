using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.Primitives;

public interface ITenantScoped
{
    TenantId TenantId { get; set; }
}