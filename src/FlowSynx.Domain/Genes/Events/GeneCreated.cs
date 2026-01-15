using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.Genes.Events;

public record GeneCreated(Gene Gene) : DomainEvent;