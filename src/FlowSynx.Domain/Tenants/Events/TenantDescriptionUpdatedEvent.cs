using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantDescriptionUpdatedEvent(TenantId TenantId) : DomainEvent;