using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.TenantSecretConfigs.Events;

public record SecretConfigDisabledEvent(Guid TenantId, Guid ConfigId) : DomainEvent;