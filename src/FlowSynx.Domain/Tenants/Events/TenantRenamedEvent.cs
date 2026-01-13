using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantRenamedEvent(Guid TenantId, string OldName, string NewName) : DomainEvent;
