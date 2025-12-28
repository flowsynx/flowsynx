using FlowSynx.Domain.Entities;

namespace FlowSynx.Application;

public interface IAuditTrailRepository
{
    Task<IReadOnlyCollection<AuditTrail>> All(CancellationToken cancellationToken);
    Task<AuditTrail?> Get(Guid id, CancellationToken cancellationToken);
}