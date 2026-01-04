using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class TenantSecretConfigRepository: ITenantSecretConfigRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<TenantSecretConfigRepository> _logger;

    public TenantSecretConfigRepository(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<TenantSecretConfigRepository> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.TenantSecretConfigs
                .AddAsync(entity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
                context.TenantSecretConfigs.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<TenantSecretConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.TenantSecretConfigs
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TenantSecretConfig by TenantId");
            return null;
        }
    }

    public async Task<TenantSecretConfig?> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.TenantSecretConfigs
                .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TenantSecretConfig by TenantId");
            return null;
        }
    }

    public async Task<IEnumerable<TenantSecretConfig>> GetEnabledConfigsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.TenantSecretConfigs
                .Where(c => c.IsEnabled)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled TenantSecretConfigs");
            return Enumerable.Empty<TenantSecretConfig>();
        }
    }

    public async Task UpdateAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(entity).State = EntityState.Detached;
            context.TenantSecretConfigs.Update(entity);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}