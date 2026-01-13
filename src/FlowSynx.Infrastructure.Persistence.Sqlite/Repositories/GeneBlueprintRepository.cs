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

    public async Task<List<GeneBlueprint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints.ToListAsync(cancellationToken);
    }

    public async Task<GeneBlueprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
            .FirstOrDefaultAsync(gb => gb.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<GeneBlueprint>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(cancellationToken);

        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
                .Where(gb =>
                    gb.Name.Contains(searchTerm) ||
                    gb.Description.Contains(searchTerm) ||
                    gb.Spec.Description.Contains(searchTerm))
                .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GeneBlueprint entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.GeneBlueprints
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GeneBlueprint entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.GeneBlueprints.Update(entity);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.GeneBlueprints.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<GeneBlueprint?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
                .FirstOrDefaultAsync(gb => gb.Name == name && gb.Version == version, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<GeneBlueprint>> GetByNamespaceAsync(string @namespace, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GeneBlueprints
            .Where(gb => gb.Namespace == @namespace)
            .ToListAsync(cancellationToken);
    }
}