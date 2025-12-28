using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.DomainEvents;

public record ChromosomeExecuted(Chromosome Chromosome, List<GeneExecutionResult> Results) : DomainEvent;
