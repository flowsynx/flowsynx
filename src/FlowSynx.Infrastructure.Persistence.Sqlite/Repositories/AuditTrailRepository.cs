using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.Domain.AuditTrails;
using FlowSynx.Application.Core.Persistence;

namespace FlowSynx.Persistence.Sqlite.Repositories;

public class AuditTrailRepository : IAuditTrailRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public AuditTrailRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<IReadOnlyCollection<AuditTrail>> All(CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var trails = await context.AuditTrails
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return trails;
    }

    public async Task<AuditTrail?> Get(long id, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var trail = await context.AuditTrails
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return trail;
    }

    public async Task<AuditTrail> Add(AuditTrail auditTrail, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var entityEntry = await context.AuditTrails
            .AddAsync(auditTrail, cancellationToken)
            .ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entityEntry.Entity;
    }
}