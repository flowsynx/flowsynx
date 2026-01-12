using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Core.Services;

public interface IGenomeManagementService
{
    Task<GeneBlueprint> RegisterGeneBlueprintAsync(string json, CancellationToken cancellationToken = default);
    Task<Chromosome> RegisterChromosomeAsync(string json, CancellationToken cancellationToken = default);
    Task<Genome> RegisterGenomeAsync(string json, CancellationToken cancellationToken = default);
    Task<ValidationResponse> ValidateJsonAsync(string json, CancellationToken cancellationToken = default);
    Task<IEnumerable<GeneBlueprint>> SearchGeneBlueprintsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chromosome>> GetChromosomesByGenomeAsync(Guid genomeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Genome>> GetGenomesByOwnerAsync(string owner, CancellationToken cancellationToken = default);
    Task<ExecutionResponse> ExecuteJsonAsync(string json, CancellationToken cancellationToken = default);
    Task<ExecutionResponse> GetExecutionResultAsync(Guid executionId, CancellationToken cancellationToken = default);
}