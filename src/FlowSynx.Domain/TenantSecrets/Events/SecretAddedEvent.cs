using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Domain.Tenants.Events;

public record SecretAddedEvent(TenantId TenantId, SecretKey Key) : DomainEvent;
