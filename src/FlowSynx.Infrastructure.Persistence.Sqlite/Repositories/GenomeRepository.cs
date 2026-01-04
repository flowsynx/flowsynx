using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class GenomeRepository : IGenomeRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<GenomeRepository> _logger;

    public GenomeRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<GenomeRepository> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<Genome>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Genomes
                .Include(g => g.Chromosomes).ThenInclude(c => c.Genes)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<Genome?> GetByIdAsync(GenomeId id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Genomes
                .Include(g => g.Chromosomes)
                    .ThenInclude(c => c.Genes)
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

    public async Task<List<Genome>> GetByMetadataAsync(string key, object value, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Genomes
                .Include(g => g.Chromosomes).ThenInclude(c => c.Genes)
                .Where(g => g.Metadata.ContainsKey(key) && g.Metadata[key].ToString() == value.ToString())
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task AddAsync(Genome entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.Genomes
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

    public async Task UpdateAsync(Genome entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(entity).State = EntityState.Detached;
            context.Genomes.Update(entity);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task DeleteAsync(GenomeId id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
                context.Genomes.Remove(entity);
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