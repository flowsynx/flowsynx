using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantActivatedEvent(TenantId TenantId) : DomainEvent;
