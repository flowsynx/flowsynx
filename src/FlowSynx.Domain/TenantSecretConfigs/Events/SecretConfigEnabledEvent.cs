using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.TenantSecretConfigs.Events;

public record SecretConfigEnabledEvent(Guid TenantId, Guid ConfigId) : DomainEvent;
