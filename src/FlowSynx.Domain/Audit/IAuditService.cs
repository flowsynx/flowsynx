namespace FlowSynx.Domain.Audit;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditEntity>> All(CancellationToken cancellationToken);
    Task<AuditEntity?> Get(Guid id, CancellationToken cancellationToken);
}