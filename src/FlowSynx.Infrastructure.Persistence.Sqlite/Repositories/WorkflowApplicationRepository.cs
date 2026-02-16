using FlowSynx.Application.Core.Persistence;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.Workflows;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class WorkflowApplicationRepository : IWorkflowApplicationRepository
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;

    public WorkflowApplicationRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
    }

    public async Task<List<WorkflowApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .Include(g => g.Workflows).ThenInclude(c => c.Activities)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowApplication?> GetByIdAsync(
        TenantId tenantId, 
        string userId, 
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .Include(g => g.Workflows)
                .ThenInclude(c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(WorkflowApplication entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        await context.WorkflowApplications
            .AddAsync(entity, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(WorkflowApplication entity, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        context.Entry(entity).State = EntityState.Detached;
        context.WorkflowApplications.Update(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(tenantId, userId, id, cancellationToken);
        if (entity != null)
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.WorkflowApplications.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<WorkflowApplication?> GetByNameAsync(string name, string @namespace = "default", CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.WorkflowApplications
            .Include(g => g.Workflows)
            .ThenInclude(c => c.Activities)
            .FirstOrDefaultAsync(g => g.Name == name && g.Namespace == @namespace, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<WorkflowApplication>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .Where(g => g.Owner == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkflowApplication>> GetByNamespaceAsync(TenantId tenantId, string userId, string @namespace, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .Where(g => g.Namespace == @namespace && g.TenantId == tenantId && g.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(TenantId tenantId, string userId, Guid workflowApplicationId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .Where(g => g.Id == workflowApplicationId && g.TenantId == tenantId && g.UserId == userId)
            .SelectMany(g => g.Workflows)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> Exist(TenantId tenantId, string userId, Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkflowApplications
            .AnyAsync(c => c.TenantId == tenantId && (c.UserId == userId) && (c.Id == id), cancellationToken);
    }
}