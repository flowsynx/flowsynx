using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.PluginConfig;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class PluginConfigurationService : IPluginConfigurationService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public PluginConfigurationService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<PluginConfigurationEntity>> All(string userId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.PluginConfiguration
            .Where(c => c.UserId == userId && c.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<PluginConfigurationEntity?> Get(string userId, Guid configId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == configId && x.IsDeleted == false, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PluginConfigurationEntity?> Get(string userId, string configName, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == configName.ToLower() && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, Guid configId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == configId && x.IsDeleted == false, cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task<bool> IsExist(string userId, string configName, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.PluginConfiguration
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == configName.ToLower() && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        await context.PluginConfiguration
            .AddAsync(configurationEntity, cancellationToken)
            .ConfigureAwait(false);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Entry(configurationEntity).State = EntityState.Detached;
        context.PluginConfiguration.Update(configurationEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(PluginConfigurationEntity configurationEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.PluginConfiguration.Remove(configurationEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var context = _appContextFactory.CreateDbContext();
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}