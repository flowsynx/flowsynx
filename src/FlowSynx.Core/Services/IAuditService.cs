using FlowSynx.Core.Models;

namespace FlowSynx.Core.Services;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditResponse>> All(CancellationToken cancellationToken);
    Task<AuditResponse?> Get(Guid id, CancellationToken cancellationToken);
}