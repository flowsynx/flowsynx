using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.GeneBlueprints.Events;

public record GeneBlueprintCreated(GeneBlueprint GeneBlueprint) : DomainEvent;