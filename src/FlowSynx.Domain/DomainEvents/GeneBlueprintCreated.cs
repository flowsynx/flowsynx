using FlowSynx.Domain.Aggregates;

namespace FlowSynx.Domain.DomainEvents;

public record GeneBlueprintCreated(GeneBlueprint GeneBlueprint) : DomainEvent;