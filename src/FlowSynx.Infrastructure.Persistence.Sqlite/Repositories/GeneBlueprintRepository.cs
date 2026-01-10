using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class GeneBlueprintRepository : IGeneBlueprintRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public GeneBlueprintRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<GeneBlueprint>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints.ToListAsync(cancellationToken);
    }

    public async Task<GeneBlueprint?> GetByIdAsync(GeneBlueprintId id, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
            .FirstOrDefaultAsync(gb => gb.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GeneBlueprint?> GetByGeneticBlueprintAsync(string geneticBlueprintId, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
            .FirstOrDefaultAsync(gb => gb.GeneticBlueprint == geneticBlueprintId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<GeneBlueprint>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(cancellationToken);

        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
            .Where(gb => gb.Phenotypic.Contains(searchTerm) ||
                            gb.Annotation.Contains(searchTerm) ||
                            gb.Id.Value.Contains(searchTerm))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.GeneBlueprints
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.GeneBlueprints.Update(entity);
    }

    public async Task DeleteAsync(GeneBlueprintId id, CancellationToken cancellationToken)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.GeneBlueprints.Remove(entity);
        }
    }

    public async Task<bool> IsEpistaticCompatibleAsync(
        GeneBlueprintId geneId, 
        string runtimeVersion, 
        string platform, 
        CancellationToken cancellationToken)
    {
        var blueprint = await GetByIdAsync(geneId, cancellationToken);
        if (blueprint == null) return false;

        var compat = blueprint.EpistaticInteraction;

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