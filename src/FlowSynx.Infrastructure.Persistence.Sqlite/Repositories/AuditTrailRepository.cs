using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.AuditTrails;
using FlowSynx.Application.Core.Interfaces;

namespace FlowSynx.Persistence.Sqlite.Repositories;

public class AuditTrailRepository : IAuditTrailRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<AuditTrailRepository> _logger;

    public AuditTrailRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory, ILogger<AuditTrailRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(appContextFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _appContextFactory = appContextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<AuditTrail>> All(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var trails = await context.AuditTrails
                .OrderByDescending(a => a.OccurredAtUtc)
                .Take(250)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return trails;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.AuditsGetList, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<AuditTrail?> Get(long id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var trail = await context.AuditTrails
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return trail;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.AuditGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<AuditTrail> Add(AuditTrail auditTrail, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var entityEntry = await context.AuditTrails
                .AddAsync(auditTrail, cancellationToken)
                .ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entityEntry.Entity;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.AuditGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}