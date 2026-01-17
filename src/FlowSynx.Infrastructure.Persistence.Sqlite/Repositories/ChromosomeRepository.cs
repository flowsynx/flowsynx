using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class ChromosomeRepository : IChromosomeRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public ChromosomeRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<Chromosome>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Where(c => c.TenantId == tenantId && (c.UserId == userId))
            .Include(c => c.Genes)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chromosome?> GetByIdAsync(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.UserId == userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Chromosome entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Chromosomes
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Chromosomes.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(tenantId, userId, id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Chromosomes.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Chromosome?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .FirstOrDefaultAsync(c => c.Name == name && c.Namespace == @namespace, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Chromosome>> GetByGenomeIdAsync(Guid genomeId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .Where(c => c.GenomeId == genomeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Chromosome>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .Where(c => c.Namespace == @namespace && c.TenantId == tenantId && c.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}