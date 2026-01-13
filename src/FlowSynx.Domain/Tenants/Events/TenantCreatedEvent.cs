using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantCreatedEvent(TenantId TenantId, string Name, string Slug) : DomainEvent;