using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantTerminatedEvent(Guid TenantId, string Reason) : DomainEvent;
