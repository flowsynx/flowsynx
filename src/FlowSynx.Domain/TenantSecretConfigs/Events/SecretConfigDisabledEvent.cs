using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.TenantSecretConfigs.Events;

public record SecretConfigDisabledEvent(TenantId TenantId, Guid ConfigId) : DomainEvent;