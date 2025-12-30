using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.DomainEvents;

public record GeneBlueprintUpdated(GeneBlueprint GeneBlueprint) : DomainEvent;