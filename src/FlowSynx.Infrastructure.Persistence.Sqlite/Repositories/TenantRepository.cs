using FlowSynx.Application;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<TenantRepository> _logger;
    private readonly IMemoryCache _cache;

    private const string CACHE_KEY_FORMAT = "config:{0}";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    public TenantRepository(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<TenantRepository> logger, 
        IMemoryCache cache)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task AddAsync(Tenant entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Tenants
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

    public async Task DeleteAsync(TenantId id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
                context.Tenants.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<List<Tenant>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Tenants.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Tenants
                .Include(t => t.Configuration)
                .Include(t => t.Contacts)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<TenantConfiguration> GetConfigurationAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(CACHE_KEY_FORMAT, tenantId.Value);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CACHE_DURATION;

            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var tenant = await context.Tenants
                                    .Include(t => t.Configuration)
                                    .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);

            var configuration = tenant?.Configuration;
            if (configuration == null)
            {
                // Create default configuration if none exists
                _logger.LogWarning("No active configuration found for tenant {TenantId}, creating default", tenantId);
                var defaultConfig = tenant.FallBackToDefaultConfiguration();
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                configuration = defaultConfig;
            }

            return configuration;
        });
    }

    public async Task<T> GetConfigurationValueAsync<T>(TenantId tenantId, string key, T defaultValue = default, CancellationToken cancellationToken = default)
    {
        var configuration = await GetConfigurationAsync(tenantId, cancellationToken);
        return configuration.GetValue(key, defaultValue);
    }

    public async Task UpdateAsync(Tenant entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(entity).State = EntityState.Detached;
            context.Tenants.Update(entity);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<ValidationResult> ValidateConfigurationAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var configuration = await GetConfigurationAsync(tenantId, cancellationToken);
        return configuration.Validate();
    }
}