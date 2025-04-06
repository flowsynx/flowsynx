using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Audit;

namespace FlowSynx.Persistence.Postgres.Services;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IDbContextFactory<ApplicationContext> appContextFactory, ILogger<AuditService> logger)
    {
        _appContextFactory = appContextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<AuditEntity>> All(CancellationToken cancellationToken)
    {
        try
        {
            using var context = _appContextFactory.CreateDbContext();
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

    public async Task<AuditEntity?> Get(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var context = _appContextFactory.CreateDbContext();
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