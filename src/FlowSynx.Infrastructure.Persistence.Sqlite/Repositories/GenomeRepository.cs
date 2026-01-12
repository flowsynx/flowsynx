using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Genomes;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class GenomeRepository : IGenomeRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public GenomeRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<Genome>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Include(g => g.Chromosomes).ThenInclude(c => c.Genes)
            .ToListAsync(cancellationToken);
    }

    public async Task<Genome?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Include(g => g.Chromosomes)
                .ThenInclude(c => c.Genes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Genome entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Genomes
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Genome entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Genomes.Update(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Genomes.Remove(entity);
        }
    }

    public async Task<Genome?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Genomes
            .Include(g => g.Chromosomes)
            .ThenInclude(c => c.Genes)
            .FirstOrDefaultAsync(g => g.Name == name && g.Namespace == @namespace);
    }

    public async Task<IEnumerable<Genome>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Where(g => g.Owner == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Genome>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Where(g => g.Namespace == @namespace)
            .ToListAsync(cancellationToken);
    }
}