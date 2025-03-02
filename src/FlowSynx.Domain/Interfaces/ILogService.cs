using FlowSynx.Domain.Entities.Logs;

namespace FlowSynx.Domain.Interfaces;

public interface ILoggerService
{
    Task<IReadOnlyCollection<Log>> All(string userId, CancellationToken cancellationToken);
    Task<Log?> Get(string userId, Guid id, CancellationToken cancellationToken);
    Task Add(Log log, CancellationToken cancellationToken);
}