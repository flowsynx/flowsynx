using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowTaskExecutionService : IWorkflowTaskExecutionService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public WorkflowTaskExecutionService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<WorkflowTaskExecutionEntity>> All(Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.WorkflowTaskExecutions
            .Where(c => c.WorkflowExecutionId == workflowExecutionId && c.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<WorkflowTaskExecutionEntity?> Get(Guid workflowTaskExecutionId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.WorkflowTaskExecutions
            .FirstOrDefaultAsync(x => x.Id == workflowTaskExecutionId && x.IsDeleted == false, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkflowTaskExecutionEntity?> Get(Guid workflowExecutionId, string taskName, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.WorkflowTaskExecutions
            .FirstOrDefaultAsync(x => x.WorkflowExecutionId == workflowExecutionId && x.Name.ToLower() == taskName.ToLower() && x.IsDeleted == false, 
            cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Add(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        await context.WorkflowTaskExecutions
            .AddAsync(workflowTaskExecutionEntity, cancellationToken)
            .ConfigureAwait(false);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Entry(workflowTaskExecutionEntity).State = EntityState.Detached;
        context.WorkflowTaskExecutions.Update(workflowTaskExecutionEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.WorkflowTaskExecutions.Remove(workflowTaskExecutionEntity);

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