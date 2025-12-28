using FlowSynx.Domain.Entities;
using System.Linq.Expressions;

namespace FlowSynx.Application;

public interface ILogEntryRepository
{
    Task<IReadOnlyCollection<LogEntry>> All(
        Expression<Func<LogEntry, bool>>? predicate, 
        CancellationToken cancellationToken);

    Task<LogEntry?> Get(
        string userId, 
        Guid id, 
        CancellationToken cancellationToken);

    Task Add(LogEntry logEntry, CancellationToken cancellationToken);

    Task<bool> CheckHealthAsync();
}