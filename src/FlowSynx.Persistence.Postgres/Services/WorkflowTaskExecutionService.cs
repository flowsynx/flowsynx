using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowTaskExecutionService : IWorkflowTaskExecutionService
{
    private readonly ApplicationContext _appContext;

    public WorkflowTaskExecutionService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<WorkflowTaskExecutionEntity>> All(Guid workflowExecutionId, CancellationToken cancellationToken)
    {
        var result = await _appContext.WorkflowTaskExecutions
            .Where(c => c.WorkflowExecutionId == workflowExecutionId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflow task executions found!");

        return result;
    }

    public async Task<WorkflowTaskExecutionEntity?> Get(Guid workflowTaskExecutionId, CancellationToken cancellationToken)
    {
        return await _appContext.WorkflowTaskExecutions
            .FirstOrDefaultAsync(x => x.Id == workflowTaskExecutionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkflowTaskExecutionEntity?> Get(Guid workflowExecutionId, string taskName, CancellationToken cancellationToken)
    {
        return await _appContext.WorkflowTaskExecutions
            .FirstOrDefaultAsync(x => x.WorkflowExecutionId == workflowExecutionId && x.Name.ToLower() == taskName.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Add(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        await _appContext.WorkflowTaskExecutions
            .AddAsync(workflowTaskExecutionEntity, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowTaskExecutions.Update(workflowTaskExecutionEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowTaskExecutionEntity workflowTaskExecutionEntity, CancellationToken cancellationToken)
    {
        _appContext.WorkflowTaskExecutions.Remove(workflowTaskExecutionEntity);

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