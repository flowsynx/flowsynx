using FlowSynx.Domain.Genomes;

namespace FlowSynx.Application.Core.Persistence;

public interface IExecutionRepository
{
    Task<List<ExecutionRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecutionRecord>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecutionRecord>> GetByTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ExecutionRecord>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task AddAsync(ExecutionRecord entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExecutionRecord entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}