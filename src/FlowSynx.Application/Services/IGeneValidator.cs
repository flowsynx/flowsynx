using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application.Services;

public interface IGeneValidator
{
    Task<ValidationResult> ValidateGeneInstanceAsync(
        GeneInstance instance, 
        GeneBlueprint blueprint, 
        CancellationToken cancellationToken);

    Task<ValidationResult> ValidateChromosomeAsync(
        Chromosome chromosome, 
        CancellationToken cancellationToken);

    Task<ValidationResult> ValidateGenomeAsync(
        Genome genome, 
        CancellationToken cancellationToken);
}