using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantContactAddedEvent(Guid TenantId, string Email, string Name, bool IsPrimary) : DomainEvent;
