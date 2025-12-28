using FlowSynx.Application;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class ChromosomeRepository : IChromosomeRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<ChromosomeRepository> _logger;

    public ChromosomeRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<ChromosomeRepository> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Chromosome>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Chromosomes
                .Include(c => c.Genes)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<Chromosome?> GetByIdAsync(ChromosomeId id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Chromosomes
                .Include(c => c.Genes)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<List<Chromosome>> GetByGenomeAsync(GenomeId genomeId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Chromosomes
                .Include(c => c.Genes)
                .Where(c => EF.Property<string>(c, "GenomeId") == genomeId.Value)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task AddAsync(Chromosome entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Chromosomes
                .AddAsync(entity, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task UpdateAsync(Chromosome entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(entity).State = EntityState.Detached;
            context.Chromosomes.Update(entity);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task DeleteAsync(ChromosomeId id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
                context.Chromosomes.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}