using FlowSynx.Application.Models;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Core.Services;

public interface IGenomeExecutionService
{
    Task<ExecutionResponse> ExecuteGeneAsync(
        TenantId tenantId,
        string userId,
        Guid geneId, 
        Dictionary<string, object> parameters, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteChromosomeAsync(
        TenantId tenantId,
        string userId,
        Guid chromosomeId, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteGenomeAsync(
        TenantId tenantId,
        string userId,
        Guid genomeId, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteRequestAsync(
        TenantId tenantId,
        string userId,
        ExecutionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ExecutionRecord?> GetExecutionRecordAsync(
        Guid executionId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ExecutionRecord>> GetExecutionHistoryAsync(
        string targetType, 
        Guid targetId,
        CancellationToken cancellationToken = default);
}