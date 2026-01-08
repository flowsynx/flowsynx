using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.GeneBlueprints.Events;

public record GeneBlueprintUpdated(GeneBlueprint GeneBlueprint) : DomainEvent;