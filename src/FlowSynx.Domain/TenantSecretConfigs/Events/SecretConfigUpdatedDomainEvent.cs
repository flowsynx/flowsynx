using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.TenantSecretConfigs.Events;

public record SecretConfigUpdatedEvent(Guid TenantId, Guid ConfigId) : DomainEvent;