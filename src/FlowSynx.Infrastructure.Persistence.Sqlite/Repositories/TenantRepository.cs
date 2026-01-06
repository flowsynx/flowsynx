using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public TenantRepository(
        IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task AddAsync(Tenant entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Tenants
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DeleteAsync(TenantId id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Tenants.Remove(entity);
        }
    }

    public Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Tenant?> GetWithConfigAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Tenants
            .Include(t => t.SecretConfigs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Tenant?> GetWithContactAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Tenants
            .Include(t => t.Contacts)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Tenant?> GetWithSecretsAsync(TenantId id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Tenants
            .Include(t => t.Secrets)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Tenant entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Tenants.Update(entity);
    }
}