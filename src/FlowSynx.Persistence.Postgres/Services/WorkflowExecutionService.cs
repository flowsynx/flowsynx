using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly ApplicationContext _appContext;

    public WorkflowExecutionService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<WorkflowExecutionEntity>> All(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        var result = await _appContext.WorkflowExecutions
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflow execution found!");

        return result;
    }

    public async Task<WorkflowExecutionEntity?> Get(string userId, Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        return await _appContext.WorkflowExecutions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowExecutionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        var result = await _appContext.WorkflowExecutions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowExecutionId, cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        await _appContext.WorkflowExecutions
            .AddAsync(workflowExecutionEntity, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowExecutions.Update(workflowExecutionEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowExecutionEntity workflowExecutionEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowExecutions.Remove(workflowExecutionEntity);

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