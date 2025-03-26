using Microsoft.EntityFrameworkCore;
using FlowSynx.Application.Services;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Application.Models;

namespace FlowSynx.Persistence.Postgres.Services;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public AuditService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<AuditResponse>> All(CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var trails = await context.Audits.Select(audit => new AuditResponse
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
        using var context = _appContextFactory.CreateDbContext();
        var trail = await context.Audits
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