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

    public async Task<IReadOnlyCollection<WorkflowDefination>> All(string userId, CancellationToken cancellationToken)
    {
        var result = await _appContext.Workflows
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.Count == 0)
            throw new Exception("No workflows found!");

        return result;
    }

    public async Task<WorkflowDefination?> Get(string userId, Guid workflowId, CancellationToken cancellationToken)
    {
        return await _appContext.Workflows
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == workflowId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<WorkflowDefination?> Get(string userId, string workflowName, CancellationToken cancellationToken)
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

    public async Task Add(WorkflowDefination workflow, CancellationToken cancellationToken)
    {
        await _appContext.Workflows
            .AddAsync(workflow, cancellationToken)
            .ConfigureAwait(false);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task Update(WorkflowDefination workflow, CancellationToken cancellationToken)
    {
        _appContext.Update(workflow);

        await _appContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> Delete(WorkflowDefination workflow, CancellationToken cancellationToken)
    {
        _appContext.Workflows.Remove(workflow);

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