using FlowSynx.Application;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.ValueObjects;
using FlowSynx.Persistence.Sqlite.Contexts;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class GeneBlueprintRepository : IGeneBlueprintRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<GeneBlueprintRepository> _logger;

    public GeneBlueprintRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<GeneBlueprintRepository> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<GeneBlueprint>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.GeneBlueprints.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<GeneBlueprint?> GetByIdAsync(GeneBlueprintId id, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.GeneBlueprints
                .FirstOrDefaultAsync(gb => gb.Id == id, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<GeneBlueprint?> GetByGeneticBlueprintAsync(string geneticBlueprintId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.GeneBlueprints
                .FirstOrDefaultAsync(gb => gb.GeneticBlueprint == geneticBlueprintId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<List<GeneBlueprint>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync(cancellationToken);

            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.GeneBlueprints
                .Where(gb => gb.Name.Contains(searchTerm) ||
                             gb.Description.Contains(searchTerm) ||
                             gb.Id.Value.Contains(searchTerm))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.GeneBlueprints
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

    public async Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(entity).State = EntityState.Detached;
            context.GeneBlueprints.Update(entity);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task DeleteAsync(GeneBlueprintId id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
                context.GeneBlueprints.Remove(entity);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationStartArgumentIsRequired, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> IsCompatibleAsync(
        GeneBlueprintId geneId, 
        string runtimeVersion, 
        string platform, 
        CancellationToken cancellationToken)
    {
        var blueprint = await GetByIdAsync(geneId, cancellationToken);
        if (blueprint == null) return false;

        var compat = blueprint.CompatibilityMatrix;

        // Check runtime version
        if (!string.IsNullOrEmpty(compat.MinimumRuntimeVersion))
        {
            var minVersion = new Version(compat.MinimumRuntimeVersion);
            var currentVersion = new Version(runtimeVersion);
            if (currentVersion < minVersion) return false;
        }

        // Check platform
        if (compat.SupportedPlatforms.Count > 0 &&
            !compat.SupportedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}