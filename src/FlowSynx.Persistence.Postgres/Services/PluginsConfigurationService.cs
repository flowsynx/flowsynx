using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.PluignConfig;

namespace FlowSynx.Persistence.Postgres.Services;

public class PluginConfigurationService : IPluginConfigurationService
{
    private readonly ApplicationContext _appContext;

    public PluginConfigurationService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<PluginConfiguration>> All(string userId, CancellationToken cancellationToken)
    {
        var result = await _appContext.PluginConfiguration
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No records found!");

        return result;
    }

    public async Task<PluginConfiguration?> Get(string userId, string configId, CancellationToken cancellationToken)
    {
        return await _appContext.PluginConfiguration
            .FindAsync(new object?[] { userId, configId }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, string configId, CancellationToken cancellationToken)
    {
        var result = await _appContext.PluginConfiguration
            .FindAsync(new object?[] { userId, configId }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        await _appContext.PluginConfiguration
            .AddAsync(configuration, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        _appContext.PluginConfiguration.Remove(configuration);

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