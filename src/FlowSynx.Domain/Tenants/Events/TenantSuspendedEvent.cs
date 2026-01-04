using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantSuspendedEvent(Guid TenantId, string Reason) : DomainEvent;
