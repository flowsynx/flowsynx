using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.Services;

public interface IGeneValidator
{
    Task<ValidationResult> ValidateGeneInstanceAsync(GeneInstance instance, GeneBlueprint blueprint);
    Task<ValidationResult> ValidateChromosomeAsync(Chromosome chromosome);
    Task<ValidationResult> ValidateGenomeAsync(Genome genome);
}