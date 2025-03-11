using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.PluginConfig;

namespace FlowSynx.Persistence.Postgres.Services;

public class PluginConfigurationService : IPluginConfigurationService
{
    private readonly ApplicationContext _appContext;

    public PluginConfigurationService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<PluginConfigurationEntity>> All(string userId, CancellationToken cancellationToken)
    {
        var result = await _appContext.PluginConfiguration
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No records found!");

        return result;
    }

    public async Task<PluginConfigurationEntity?> Get(string userId, Guid configId, CancellationToken cancellationToken)
    {
        return await _appContext.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == configId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PluginConfigurationEntity?> Get(string userId, string configName, CancellationToken cancellationToken)
    {
        return await _appContext.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == configName.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, Guid configId, CancellationToken cancellationToken)
    {
        var result = await _appContext.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == configId, cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task<bool> IsExist(string userId, string configName, CancellationToken cancellationToken)
    {
        var result = await _appContext.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == configName.ToLower(), cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        await _appContext.PluginConfiguration
            .AddAsync(configurationEntity, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        _appContext.PluginConfiguration.Update(configurationEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        _appContext.PluginConfiguration.Remove(configurationEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _appContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}