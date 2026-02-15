using FlowSynx.Domain.WorkflowExecutions;

namespace FlowSynx.Application.Core.Persistence;

public interface IWorkflowExecutionRepository
{
    Task<List<WorkflowExecution>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WorkflowExecution?> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowExecution>> GetByStatusAsync(
        string status, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowExecution>> GetByTargetAsync(
        string targetType, 
        Guid targetId, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<WorkflowExecution>> GetRecentAsync(
        int count = 50, 
        CancellationToken cancellationToken = default);

    Task AddAsync(WorkflowExecution entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowExecution entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}