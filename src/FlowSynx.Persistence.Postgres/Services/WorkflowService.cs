using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using FlowSynx.Persistence.Postgres.Extensions;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public WorkflowService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<WorkflowEntity>> All(string userId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.Workflows
            .Where(c => c.UserId == userId && c.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<WorkflowEntity?> Get(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.Workflows.Include(x=>x.Triggers).Include(x=>x.Executions).ThenInclude(x=>x.TaskExecutions)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowId && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkflowEntity?> Get(string userId, string workflowName, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.Workflows
            .FirstOrDefaultAsync(x=>x.UserId == userId && x.Name.ToLower() == workflowName.ToLower() && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, string workflowName, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.Workflows
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == workflowName.ToLower() && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        await context.Workflows
            .AddAsync(workflowEntity, cancellationToken)
            .ConfigureAwait(false);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Entry(workflowEntity).State = EntityState.Detached;
        context.Workflows.Update(workflowEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Workflows.Remove(workflowEntity);
        context.SoftDelete(workflowEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var context = _appContextFactory.CreateDbContext();
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}