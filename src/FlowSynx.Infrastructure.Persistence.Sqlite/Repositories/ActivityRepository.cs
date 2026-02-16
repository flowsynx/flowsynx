using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public ActivityRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<Activity>> GetAllAsync(
        TenantId tenantId,
        string userId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Activities
            .Where(g => g.TenantId == tenantId && g.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Activity?> GetByIdAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Activities
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId && g.UserId == userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<Activity>> SearchAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(tenantId, userId, cancellationToken);

        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Activities
                .Where(g =>
                    g.Name.Contains(searchTerm) ||
                    g.Description.Contains(searchTerm) ||
                    g.Specification.Description.Contains(searchTerm))
                .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Activity entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Activities
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Activity entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Activities.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(tenantId, userId, id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Activities.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Activity?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Activities
                .FirstOrDefaultAsync(g => g.Name == name && g.Version == version, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Activity>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Activities
            .Where(g => g.Namespace == @namespace && g.TenantId == tenantId && g.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}