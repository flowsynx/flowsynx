using FlowSynx.Domain.Entities.Logs;
using System.Linq.Expressions;

namespace FlowSynx.Domain.Interfaces;

public interface ILoggerService
{
    Task<IReadOnlyCollection<Log>> All(Expression<Func<Log, bool>>? predicate, CancellationToken cancellationToken);
    Task<Log?> Get(string userId, Guid id, CancellationToken cancellationToken);
    Task Add(Log log, CancellationToken cancellationToken);
}