using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowTriggerService : IWorkflowTriggerService
{
    private readonly ApplicationContext _appContext;

    public WorkflowTriggerService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(CancellationToken cancellationToken)
    {
        var result = await _appContext.WorkflowTriggeres
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflows found!");

        return result;
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(WorkflowTriggerType type, CancellationToken cancellationToken)
    {
        var result = await _appContext.WorkflowTriggeres
            .Where(x=>x.Type == type)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflow trigger found!");

        return result;
    }

    public async Task<WorkflowTriggerEntity?> Get(Guid workflowTriggerId, CancellationToken cancellationToken)
    {
        return await _appContext.WorkflowTriggeres
            .FirstOrDefaultAsync(x=>x.Id == workflowTriggerId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Add(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        await _appContext.WorkflowTriggeres
            .AddAsync(workflowTriggerEntity, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowTriggeres.Update(workflowTriggerEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowTriggeres.Remove(workflowTriggerEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _appContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}