using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantContactAddedEvent(TenantId TenantId, string Email, string Name, bool IsPrimary) : DomainEvent;
