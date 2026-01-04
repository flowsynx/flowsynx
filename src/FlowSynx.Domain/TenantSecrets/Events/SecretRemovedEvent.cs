using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Domain.Tenants.Events;

public record SecretRemovedEvent(TenantId TenantId, SecretKey Key) : DomainEvent;
