using System.Linq.Expressions;

namespace FlowSynx.Domain.Log;

public interface ILoggerService
{
    Task<IReadOnlyCollection<LogEntity>> All(Expression<Func<LogEntity, bool>>? predicate, 
        CancellationToken cancellationToken);

    Task<LogEntity?> Get(string userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LogEntity>> GetWorkflowExecutionLogs(string userId, Guid workflowId, 
        Guid workflowExecutionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LogEntity>> GetWorkflowTaskExecutionLogs(string userId, Guid workflowId,
        Guid workflowExecutionId, Guid workflowTaskExecutionId, CancellationToken cancellationToken);

    Task Add(LogEntity logEntity, CancellationToken cancellationToken);

    Task<bool> CheckHealthAsync();
}