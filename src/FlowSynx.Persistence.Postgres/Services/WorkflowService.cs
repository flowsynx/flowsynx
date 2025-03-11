using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Domain.Interfaces;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowService : IWorkflowService
{
    private readonly ApplicationContext _appContext;

    public WorkflowService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public async Task<IReadOnlyCollection<WorkflowEntity>> All(string userId, CancellationToken cancellationToken)
    {
        var result = await _appContext.Workflows
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflows found!");

        return result;
    }

    public async Task<WorkflowEntity?> Get(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        return await _appContext.Workflows
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkflowEntity?> Get(string userId, string workflowName, CancellationToken cancellationToken)
    {
        return await _appContext.Workflows
            .FirstOrDefaultAsync(x=>x.UserId == userId && x.Name.ToLower() == workflowName.ToLower(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsExist(string userId, string workflowName, CancellationToken cancellationToken)
    {
        var result = await _appContext.Workflows
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == workflowName.ToLower(), cancellationToken)
            .ConfigureAwait(false);

        return result != null;
    }

    public async Task Add(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        await _appContext.Workflows
            .AddAsync(workflowEntity, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        _appContext.Workflows.Update(workflowEntity);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowEntity workflowEntity, CancellationToken cancellationToken)
    {
        _appContext.Workflows.Remove(workflowEntity);

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