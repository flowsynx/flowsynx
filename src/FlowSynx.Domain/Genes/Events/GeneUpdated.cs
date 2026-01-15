using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Genes.Events;

public record GeneUpdated(Gene Gene) : DomainEvent;