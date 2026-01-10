using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Core.Services;

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