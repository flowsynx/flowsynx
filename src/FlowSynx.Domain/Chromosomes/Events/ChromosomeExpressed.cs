using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Chromosomes.Events;

public record ChromosomeExpressed(Chromosome Chromosome, List<ExpressionResult> Results) : DomainEvent;
