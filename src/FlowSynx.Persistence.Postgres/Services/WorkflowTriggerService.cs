using Microsoft.EntityFrameworkCore;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Trigger;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowTriggerService : IWorkflowTriggerService
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;
    private readonly ILogger<WorkflowTriggerService> _logger;

    public WorkflowTriggerService(IDbContextFactory<ApplicationContext> appContextFactory,
        ILogger<WorkflowTriggerService> logger)
    {
        ArgumentNullException.ThrowIfNull(appContextFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _appContextFactory = appContextFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> All(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.WorkflowTriggeres
                .Where(x => x.IsDeleted == false)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowTriggersList, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<IReadOnlyCollection<WorkflowTriggerEntity>> ActiveTriggers(WorkflowTriggerType type, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var result = await context.WorkflowTriggeres
                .Where(x => x.Type == type && x.Status == WorkflowTriggerStatus.Active && x.IsDeleted == false)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowActiveTriggersList, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<WorkflowTriggerEntity?> Get(Guid workflowTriggerId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.WorkflowTriggeres
                .FirstOrDefaultAsync(x => x.Id == workflowTriggerId && x.IsDeleted == false, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowGetTriggerItem, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task Add(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            await context.WorkflowTriggeres
                .AddAsync(workflowTriggerEntity, cancellationToken)
                .ConfigureAwait(false);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowTriggersAdd, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task Update(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.Entry(workflowTriggerEntity).State = EntityState.Detached;
            context.WorkflowTriggeres.Update(workflowTriggerEntity);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowTriggersUpdate, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> Delete(WorkflowTriggerEntity workflowTriggerEntity, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            context.WorkflowTriggeres.Remove(workflowTriggerEntity);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowTriggersDelete, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}