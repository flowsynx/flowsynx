using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantSuspendedEvent(TenantId TenantId, string Reason) : DomainEvent;
