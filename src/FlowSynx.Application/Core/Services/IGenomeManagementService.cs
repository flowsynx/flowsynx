using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Services;

public interface IGenomeManagementService
{
    Task<Gene> RegisterGeneAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<Chromosome> RegisterChromosomeAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<Genome> RegisterGenomeAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<ValidationResponse> ValidateJsonAsync(
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Gene>> SearchGenesAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Chromosome>> GetChromosomesByGenomeAsync(
        string userId, 
        Guid genomeId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Genome>> GetGenomesByOwnerAsync(
        string userId, 
        string owner, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteJsonAsync(
        TenantId tenantId,
        string userId, 
        string json, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> GetExecutionResultAsync(
        string userId, 
        Guid executionId, 
        CancellationToken cancellationToken = default);
}