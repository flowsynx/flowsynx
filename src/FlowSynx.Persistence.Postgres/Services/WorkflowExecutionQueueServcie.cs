using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Workflow;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Persistence.Postgres.Services;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Persistence.Postgres.Services;

public class WorkflowExecutionQueueServcie : IWorkflowExecutionQueue
{
    private readonly IDbContextFactory<ApplicationContext> _appContextFactory;
    private readonly ILogger<WorkflowTaskExecutionService> _logger;

    public WorkflowExecutionQueueServcie(IDbContextFactory<ApplicationContext> appContextFactory,
        ILogger<WorkflowTaskExecutionService> logger)
    {
        ArgumentNullException.ThrowIfNull(appContextFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _appContextFactory = appContextFactory;
        _logger = logger;
    }

    public async ValueTask EnqueueAsync(
        ExecutionQueueRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

            var workflowQueueEntity = new WorkflowQueueEntity
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                WorkflowId = request.WorkflowId,
                ExecutionId = request.ExecutionId,
                Status = WorkflowQueueStatus.Pending,
            };

            await context.WorkflowQueue
                .AddAsync(workflowQueueEntity, cancellationToken)
                .ConfigureAwait(false);

            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.WorkflowQueueAdd, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public async IAsyncEnumerable<ExecutionQueueRequest> DequeueAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            // Pull oldest pending job
            var entity = await context.Set<WorkflowQueueEntity>()
                .Where(x => x.Status == WorkflowQueueStatus.Pending)
                .OrderBy(x => x.CreatedOn)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity != null)
            {
                entity.Status = WorkflowQueueStatus.Processing;
                await context.SaveChangesAsync(cancellationToken);

                yield return new ExecutionQueueRequest(
                    entity.UserId,
                    entity.WorkflowId,
                    entity.ExecutionId,
                    cancellationToken);
            }
            else
            {
                await Task.Delay(1000, cancellationToken); // wait before polling again
            }
        }
    }

    public async Task CompleteAsync(Guid executionId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.WorkflowQueue
                .FirstOrDefaultAsync(x => x.ExecutionId == executionId, cancellationToken);

            if (entity != null)
            {
                entity.Status = WorkflowQueueStatus.Completed;
                await context.SaveChangesAsync(cancellationToken);
                context.WorkflowQueue.Remove(entity);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    public async Task FailAsync(Guid executionId, CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.WorkflowQueue
                .FirstOrDefaultAsync(x => x.ExecutionId == executionId, cancellationToken);

            if (entity != null)
            {
                entity.Status = WorkflowQueueStatus.Failed;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}