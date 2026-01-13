using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class TenantSecretConfigRepository: ITenantSecretConfigRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public TenantSecretConfigRepository(
        IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task AddAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.TenantSecretConfigs
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.TenantSecretConfigs.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<TenantSecretConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TenantSecretConfigs
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<TenantSecretConfig?> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TenantSecretConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<TenantSecretConfig>> GetEnabledConfigsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TenantSecretConfigs
            .Where(c => c.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(TenantSecretConfig entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.TenantSecretConfigs.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}