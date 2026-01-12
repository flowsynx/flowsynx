using FlowSynx.Application.Models;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public interface IGenomeExecutionService
{
    Task<ExecutionResponse> ExecuteGeneAsync(
        Guid geneBlueprintId, 
        Dictionary<string, object> parameters, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteChromosomeAsync(
        Guid chromosomeId, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteGenomeAsync(
        Guid genomeId, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);

    Task<ExecutionResponse> ExecuteRequestAsync(
        ExecutionRequest request, 
        CancellationToken cancellationToken = default);

    Task<ExecutionRecord?> GetExecutionRecordAsync(
        Guid executionId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ExecutionRecord>> GetExecutionHistoryAsync(
        string targetType, 
        Guid targetId, 
        int limit = 50, 
        CancellationToken cancellationToken = default);
}