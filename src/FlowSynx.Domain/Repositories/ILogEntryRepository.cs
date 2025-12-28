using FlowSynx.Domain.Entities;
using System.Linq.Expressions;

namespace FlowSynx.Domain.Repositories;

public interface ILogEntryRepository
{
    Task<IReadOnlyCollection<LogEntry>> All(Expression<Func<LogEntry, bool>>? predicate, 
        CancellationToken cancellationToken);

    Task<LogEntry?> Get(string userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LogEntry>> GetWorkflowExecutionLogs(string userId, Guid workflowId, 
        Guid workflowExecutionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LogEntry>> GetWorkflowTaskExecutionLogs(string userId, Guid workflowId,
        Guid workflowExecutionId, Guid workflowTaskExecutionId, CancellationToken cancellationToken);

    Task Add(LogEntry logEntry, CancellationToken cancellationToken);

    Task<bool> CheckHealthAsync();
}