using FlowSynx.Domain.AuditTrails;

namespace FlowSynx.Application;

public interface IAuditTrailRepository
{
    Task<IReadOnlyCollection<AuditTrail>> All(CancellationToken cancellationToken);
    Task<AuditTrail?> Get(long id, CancellationToken cancellationToken);
    Task<AuditTrail> Add(AuditTrail auditTrail, CancellationToken cancellationToken);
}