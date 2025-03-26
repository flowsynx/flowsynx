using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowTriggerService : IWorkflowTriggerService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;

    public WorkflowTriggerService(IDbContextFactory<ApplicationContext> appContextFactory)
    {
        _appContextFactory = appContextFactory;
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.WorkflowTriggeres
            .Where(x => x.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> ActiveTriggers(WorkflowTriggerType type, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        var result = await context.WorkflowTriggeres
            .Where(x=>x.Type == type && x.Status == WorkflowTriggerStatus.Active && x.IsDeleted == false)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    public async Task<WorkflowTriggerEntity?> Get(Guid workflowTriggerId, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        return await context.WorkflowTriggeres
            .FirstOrDefaultAsync(x=>x.Id == workflowTriggerId && x.IsDeleted == false, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Add(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        await context.WorkflowTriggeres
            .AddAsync(workflowTriggerEntity, cancellationToken)
            .ConfigureAwait(false);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.Entry(workflowTriggerEntity).State = EntityState.Detached;
        context.WorkflowTriggeres.Update(workflowTriggerEntity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        using var context = _appContextFactory.CreateDbContext();
        context.WorkflowTriggeres.Remove(workflowTriggerEntity);

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