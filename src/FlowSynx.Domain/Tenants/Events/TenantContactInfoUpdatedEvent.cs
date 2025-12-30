using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantContactInfoUpdatedEvent(TenantId TenantId) : DomainEvent;