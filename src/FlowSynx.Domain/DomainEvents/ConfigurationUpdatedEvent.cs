using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.DomainEvents;

public record ConfigurationUpdatedEvent(string key) : DomainEvent;
