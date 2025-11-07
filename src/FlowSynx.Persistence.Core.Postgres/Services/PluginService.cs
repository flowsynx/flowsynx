using Microsoft.Extensions.Logging;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Persistence.Core.Postgres.Contexts;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Core.Postgres.Extensions;
using FlowSynx.Domain.Plugin;

namespace FlowSynx.Persistence.Core.Postgres.Services;

public class PluginService : IPluginService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;
    private readonly ILogger<PluginService> _logger;

    public PluginService(IDbContextFactory<ApplicationContext> appContextFactory, ILogger<PluginService> logger)
    {
        ArgumentNullException.ThrowIfNull(appContextFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _appContextFactory = appContextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<PluginEntity>> All(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Plugins
                .Where(c => c.UserId == userId && c.IsDeleted == false)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginsGetList, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<PluginEntity?> Get(string userId, Guid pluginId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Plugins
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == pluginId && !x.IsDeleted, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<PluginEntity?> Get(string userId, string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Plugins
                .FirstOrDefaultAsync(
                    x => x.UserId == userId 
                    && x.Type.ToLower() == pluginType.ToLower() 
                    && x.Version == pluginVersion 
                    && !x.IsDeleted, cancellationToken
                ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginGetItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> IsExist(string userId, string pluginType, string pluginVersion, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.Plugins
                .FirstOrDefaultAsync(
                    x => x.UserId == userId 
                    && x.Type.ToLower() == pluginType.ToLower()
                    && x.Version == pluginVersion
                    && !x.IsDeleted, cancellationToken
                ).ConfigureAwait(false);

            return result != null;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginCheckExistence, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task Add(PluginEntity pluginEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Plugins
                .AddAsync(pluginEntity, cancellationToken)
                .ConfigureAwait(false);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginAdd, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> Delete(PluginEntity pluginEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Plugins.Remove(pluginEntity);
            context.SoftDelete(pluginEntity);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginDelete, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}