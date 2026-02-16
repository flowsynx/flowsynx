using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Workflows;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public WorkflowRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<Workflow>> GetAllAsync(
        TenantId tenantId, 
        string userId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Where(c => c.TenantId == tenantId && (c.UserId == userId))
            .Include(c => c.Activities)
            .ToListAsync(cancellationToken);
    }

    public async Task<Workflow?> GetByIdAsync(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.UserId == userId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Workflow entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Workflows
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Workflow entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.Workflows.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(tenantId, userId, id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Workflows.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Workflow?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .FirstOrDefaultAsync(c => c.Name == name && c.Namespace == @namespace, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Workflow>> GetByGenomeIdAsync(Guid genomeId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Include(c => c.Activities)
            .Where(c => c.WorkflowApplicationId == genomeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Workflow>> GetByNamespaceAsync(
        TenantId tenantId,
        string userId, 
        string @namespace, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Include(c => c.Activities)
            .Where(c => c.Namespace == @namespace && c.TenantId == tenantId && c.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Domain.ActivityInstances.ActivityInstance>> GetWorkflowActivitiesAsync(
        TenantId tenantId, 
        string userId, 
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Where(c => c.TenantId == tenantId && (c.UserId == userId) && (c.Id == workflowId))
            .Include(c => c.Activities)
            .SelectMany(c => c.Activities)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> Exist(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .AnyAsync(c => c.TenantId == tenantId && (c.UserId == userId) && (c.Id == id), cancellationToken);
    }

    public async Task<IEnumerable<Workflow>> GetByWorkflowApplicationIdAsync(
        Guid workflowApplicationId, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Workflows
            .Include(c => c.Activities)
            .Where(c => c.WorkflowApplicationId == workflowApplicationId)
            .ToListAsync(cancellationToken);
    }
}