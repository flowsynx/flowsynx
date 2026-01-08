using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Chromosomes.Events;

public record ChromosomeExecuted(Chromosome Chromosome, List<GeneExecutionResult> Results) : DomainEvent;
