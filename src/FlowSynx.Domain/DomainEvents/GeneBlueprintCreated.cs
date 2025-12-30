using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.DomainEvents;

public record GeneBlueprintCreated(GeneBlueprint GeneBlueprint) : DomainEvent;