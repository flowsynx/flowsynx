using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Persistence.Postgres.Contexts;
using FlowSynx.Persistence.Postgres.Services;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace FlowSynx.Infrastructure.Workflow;

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

    public async ValueTask QueueExecutionAsync(
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
                TriggerPayload = SerializeTrigger(request.Trigger)
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

                var trigger = DeserializeTrigger(entity.TriggerPayload);

                yield return new ExecutionQueueRequest(
                    entity.UserId,
                    entity.WorkflowId,
                    entity.ExecutionId,
                    cancellationToken,
                    trigger);
            }
            else
            {
                await Task.Delay(1000, cancellationToken); // wait before polling again
            }
        }
    }

    public async Task MarkAsCompletedAsync(Guid executionId, CancellationToken cancellationToken)
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

    public async Task MarkAsFailedAsync(Guid executionId, CancellationToken cancellationToken)
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

    private static string? SerializeTrigger(WorkflowTrigger? trigger)
    {
        if (trigger == null)
            return null;

        var snapshot = new WorkflowTriggerSnapshot
        {
            Type = trigger.Type,
            Properties = new Dictionary<string, object>(trigger.Properties, StringComparer.OrdinalIgnoreCase)
        };

        return JsonConvert.SerializeObject(snapshot);
    }

    private static WorkflowTrigger? DeserializeTrigger(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        var snapshot = JsonConvert.DeserializeObject<WorkflowTriggerSnapshot>(payload);
        if (snapshot == null)
            return null;

        var normalized = NormalizeDictionary(snapshot.Properties);
        return new WorkflowTrigger
        {
            Type = snapshot.Type,
            Properties = normalized
        };
    }

    private static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> source)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            result[pair.Key] = NormalizeValue(pair.Value);
        }

        return result;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            null => null,
            JToken token => NormalizeToken(token),
            IDictionary<string, object> dict => dict.ToDictionary(
                kvp => kvp.Key,
                kvp => NormalizeValue(kvp.Value),
                StringComparer.OrdinalIgnoreCase),
            IList list => list.Cast<object?>().Select(NormalizeValue).ToList(),
            _ => value
        };
    }

    private static object? NormalizeToken(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => token.Children<JProperty>()
                .ToDictionary(prop => prop.Name, prop => NormalizeToken(prop.Value), StringComparer.OrdinalIgnoreCase),
            JTokenType.Array => token.Children().Select(NormalizeToken).ToList(),
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.String => token.Value<string>(),
            JTokenType.Null => null,
            _ => token.ToString()
        };
    }

    private sealed class WorkflowTriggerSnapshot
    {
        public WorkflowTriggerType Type { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
