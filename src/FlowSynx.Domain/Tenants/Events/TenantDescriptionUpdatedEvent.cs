using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantDescriptionUpdatedEvent(Guid TenantId) : DomainEvent;