using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Tenants.Events;

public record TenantConfigurationCreatedEvent(TenantId TenantId) : DomainEvent;