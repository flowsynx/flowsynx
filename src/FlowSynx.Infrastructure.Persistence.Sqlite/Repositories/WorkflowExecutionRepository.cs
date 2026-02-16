using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.WorkflowExecutions;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public WorkflowExecutionRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task AddAsync(WorkflowExecution entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.WorkflowExecutions
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
            context.WorkflowExecutions.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<List<WorkflowExecution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowExecutions
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowExecution>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowExecutions
            .Include(er => er.Logs)
            .Include(er => er.Artifacts)
            .Where(er => er.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkflowExecution>> GetByTargetAsync(string targetType, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowExecutions
            .Include(er => er.Logs)
            .Include(er => er.Artifacts)
            .Where(er => er.TargetType == targetType && er.TargetId == targetId)
            .OrderByDescending(er => er.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkflowExecution>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowExecutions
            .Include(er => er.Logs)
            .OrderByDescending(er => er.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkflowExecution entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.WorkflowExecutions.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}