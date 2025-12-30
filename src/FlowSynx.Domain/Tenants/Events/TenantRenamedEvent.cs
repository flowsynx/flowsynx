using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantRenamedEvent(TenantId TenantId, string OldName, string NewName) : DomainEvent;
