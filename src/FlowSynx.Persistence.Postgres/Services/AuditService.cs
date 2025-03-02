using Microsoft.EntityFrameworkCore;
using FlowSynx.Core.Services;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Core.Models;

namespace FlowSynx.Persistence.Postgres.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationContext _appContext;

    public AuditService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<AuditResponse>> All(CancellationToken cancellationToken)
    {
        var trails = await _appContext.AuditTrails.Select(audit => new AuditResponse
                {
                    Id = audit.Id,
                    Type = audit.Type,
                    TableName = audit.TableName,
                    DateTime = audit.DateTime,
                    OldValues = audit.OldValues,
                    NewValues = audit.NewValues,
                    AffectedColumns = audit.AffectedColumns,
                    PrimaryKey = audit.PrimaryKey
                })
            .OrderByDescending(a => a.DateTime).Take(250)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return trails;
    }

    public async Task<AuditResponse?> Get(Guid id, CancellationToken cancellationToken)
    {
        var trail = await _appContext.AuditTrails
            .Where(x=>x.Id == id)
            .Select(audit => new AuditResponse
                {
                    Id = audit.Id,
                    Type = audit.Type,
                    TableName = audit.TableName,
                    DateTime = audit.DateTime,
                    OldValues = audit.OldValues,
                    NewValues = audit.NewValues,
                    AffectedColumns = audit.AffectedColumns,
                    PrimaryKey = audit.PrimaryKey
                })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return trail;
    }
}