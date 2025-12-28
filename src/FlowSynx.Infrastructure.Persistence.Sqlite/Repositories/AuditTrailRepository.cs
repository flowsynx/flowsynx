using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Entities;
using FlowSynx.Application;

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
            var trails = await context.Audits
                .OrderByDescending(a => a.DateTime)
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

    public async Task<AuditTrail?> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var trail = await context.Audits
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
}