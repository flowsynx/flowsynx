using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.DomainEvents;

public record ChromosomeExecuted(Chromosome Chromosome, List<GeneExecutionResult> Results) : DomainEvent;
