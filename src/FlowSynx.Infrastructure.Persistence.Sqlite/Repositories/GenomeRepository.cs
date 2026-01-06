using FlowSynx.Application.Abstractions.Persistence;
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

    public async Task<List<Genome>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Include(g => g.Chromosomes).ThenInclude(c => c.Genes)
            .ToListAsync(cancellationToken);
    }

    public async Task<Genome?> GetByIdAsync(GenomeId id, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Include(g => g.Chromosomes)
                .ThenInclude(c => c.Genes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<Genome>> GetByMetadataAsync(string key, object value, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Genomes
            .Include(g => g.Chromosomes).ThenInclude(c => c.Genes)
            .Where(g => g.Metadata.ContainsKey(key) && g.Metadata[key].ToString() == value.ToString())
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Genome entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Genomes
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Genome entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Genomes.Update(entity);
    }

    public async Task DeleteAsync(GenomeId id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Genomes.Remove(entity);
        }
    }
}