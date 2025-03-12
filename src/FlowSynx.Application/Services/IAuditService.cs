using FlowSynx.Application.Models;

namespace FlowSynx.Application.Services;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditResponse>> All(CancellationToken cancellationToken);
    Task<AuditResponse?> Get(Guid id, CancellationToken cancellationToken);
}