using FlowSynx.Domain.Aggregates;

namespace FlowSynx.Domain.DomainEvents;

public record GeneBlueprintUpdated(GeneBlueprint GeneBlueprint) : DomainEvent;