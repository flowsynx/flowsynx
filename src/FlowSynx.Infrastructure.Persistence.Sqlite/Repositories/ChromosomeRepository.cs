using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
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

    public async Task<List<Chromosome>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .ToListAsync(cancellationToken);
    }

    public async Task<Chromosome?> GetByIdAsync(ChromosomeId id, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<Chromosome>> GetByGenomeAsync(GenomeId genomeId, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Chromosomes
            .Include(c => c.Genes)
            .Where(c => EF.Property<string>(c, "GenomeId") == genomeId.Value)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Chromosome entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Chromosomes
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Chromosomes.Update(entity);
    }

    public async Task DeleteAsync(ChromosomeId id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Chromosomes.Remove(entity);
        }
    }
}