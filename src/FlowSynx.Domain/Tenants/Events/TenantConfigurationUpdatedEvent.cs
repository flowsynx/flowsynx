using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantConfigurationUpdatedEvent(
    TenantId TenantId, 
    string ChangeReason) : DomainEvent;
