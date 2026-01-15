using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class GeneRepository : IGeneRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public GeneRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<Gene>> GetAllAsync(
        TenantId tenantId,
        string userId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genes
            .Where(g => g.TenantId == tenantId && g.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Gene?> GetByIdAsync(
        TenantId tenantId,
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genes
            .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId && g.UserId == userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<Gene>> SearchAsync(
        TenantId tenantId,
        string userId, 
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(tenantId, userId, cancellationToken);

        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genes
                .Where(g =>
                    g.Name.Contains(searchTerm) ||
                    g.Description.Contains(searchTerm) ||
                    g.Specification.Description.Contains(searchTerm))
                .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Gene entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Genes
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Gene entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Genes.Update(entity);

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
            context.Genes.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Gene?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genes
                .FirstOrDefaultAsync(g => g.Name == name && g.Version == version, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Gene>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genes
            .Where(g => g.Namespace == @namespace)
            .ToListAsync(cancellationToken);
    }
}