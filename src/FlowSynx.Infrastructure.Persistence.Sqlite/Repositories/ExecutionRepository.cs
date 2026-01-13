using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Genomes;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class ExecutionRepository : IExecutionRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public ExecutionRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task AddAsync(ExecutionRecord entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.ExecutionRecords
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.ExecutionRecords.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<List<ExecutionRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExecutionRecords
            .ToListAsync(cancellationToken);
    }

    public async Task<ExecutionRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExecutionRecords
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExecutionRecords
            .Include(er => er.Logs)
            .Include(er => er.Artifacts)
            .Where(er => er.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetByTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExecutionRecords
            .Include(er => er.Logs)
            .Include(er => er.Artifacts)
            .Where(er => er.TargetType == targetType && er.TargetId == targetId)
            .OrderByDescending(er => er.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ExecutionRecords
            .Include(er => er.Logs)
            .OrderByDescending(er => er.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExecutionRecord entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.ExecutionRecords.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}