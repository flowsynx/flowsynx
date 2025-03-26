using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public WorkflowExecutionService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<WorkflowExecutionEntity>> All(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.WorkflowExecutions
            .Where(c => c.UserId == userId && c.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<WorkflowExecutionEntity?> Get(string userId, Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.WorkflowExecutions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowExecutionId && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.WorkflowExecutions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowExecutionId && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        await context.WorkflowExecutions
            .AddAsync(workflowExecutionEntity, cancellationToken)
            .ConfigureAwait(false);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Entry(workflowExecutionEntity).State = EntityState.Detached;
        context.WorkflowExecutions.Update(workflowExecutionEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.WorkflowExecutions.Remove(workflowExecutionEntity);

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