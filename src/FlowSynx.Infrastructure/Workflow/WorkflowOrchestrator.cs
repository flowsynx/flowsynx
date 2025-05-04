using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly IWorkflowExecutionTracker _executionTracker;
    private readonly IWorkflowTaskExecutor _taskExecutor;
    private readonly IExpressionParserFactory _parserFactory;
    private readonly ISemaphoreFactory _semaphoreFactory;
    private readonly ConcurrentDictionary<string, object?> _taskOutputs = new();

    public WorkflowOrchestrator(
        ILogger<WorkflowOrchestrator> logger,
        IWorkflowExecutionTracker executionTracker,
        IWorkflowTaskExecutor taskExecutor,
        IExpressionParserFactory parserFactory,
        ISemaphoreFactory semaphoreFactory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(executionTracker);
        ArgumentNullException.ThrowIfNull(taskExecutor);
        ArgumentNullException.ThrowIfNull(parserFactory);
        ArgumentNullException.ThrowIfNull(semaphoreFactory);
        _logger = logger;
        _executionTracker = executionTracker;
        _taskExecutor = taskExecutor;
        _parserFactory = parserFactory;
        _semaphoreFactory = semaphoreFactory;
    }

    public async Task ExecuteWorkflowAsync(
        string userId, 
        Guid executionId,
        WorkflowDefinition definition, 
        CancellationToken cancellationToken)
    {
        var taskMap = definition.Tasks.ToDictionary(t => t.Name);
        var pendingTasks = new HashSet<string>(taskMap.Keys);

        while (pendingTasks.Any())
        {
            var readyTasks = pendingTasks
                .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d)))
                .ToList();

            if (!readyTasks.Any())
                throw new FlowSynxException((int)ErrorCode.WorkflowFailedDependenciesTask,
                    Resources.Workflow_Executor_FailedDependenciesTask);

            var tasksToExecute = readyTasks.Select(taskId => taskMap[taskId]);

            var config = definition.Configuration;
            var parser = _parserFactory.CreateParser(_taskOutputs.ToDictionary());
            var errors = await ExecuteTasksAsync(userId, executionId, tasksToExecute, parser, config, cancellationToken)
                              .ConfigureAwait(false);

            if (errors.Any())
                HandleTaskExecutionErrors(errors);

            foreach (var taskId in readyTasks)
                pendingTasks.Remove(taskId);
        }
    }

    private async Task<List<Exception>> ExecuteTasksAsync(
        string userId,
        Guid workflowExecutionId,
        IEnumerable<WorkflowTask> tasks,
        IExpressionParser parser,
        WorkflowConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var exceptions = new ConcurrentBag<Exception>();
        var semaphore = _semaphoreFactory.Create(configuration.DegreeOfParallelism ?? 3);

        using var globalCts = new CancellationTokenSource();
        if (configuration.Timeout.HasValue)
            globalCts.CancelAfter(TimeSpan.FromMilliseconds(configuration.Timeout.Value));

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(globalCts.Token, cancellationToken);

        var executionTasks = tasks.Select(async task =>
        {
            await semaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            try
            {
                await _executionTracker.UpdateTaskStatusAsync(workflowExecutionId, task.Name,
                    WorkflowTaskExecutionStatus.Running, linkedCts.Token).ConfigureAwait(false);

                var result = await _taskExecutor.ExecuteAsync(userId, task, parser, linkedCts.Token);
                _taskOutputs[task.Name] = result;

                await _executionTracker.UpdateTaskStatusAsync(workflowExecutionId, task.Name,
                    WorkflowTaskExecutionStatus.Completed, linkedCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _executionTracker.UpdateTaskStatusAsync(workflowExecutionId, task.Name,
                    WorkflowTaskExecutionStatus.Failed, linkedCts.Token).ConfigureAwait(false);

                exceptions.Add(new Exception(string.Format(Resources.WorkflowOrchestrator_TaskFailed, task.Name, ex.Message), ex));
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(executionTasks).ConfigureAwait(false);
        return exceptions.ToList();
    }

    private static void HandleTaskExecutionErrors(List<Exception> exceptions)
    {
        var errorMessage = new ErrorMessage(
            (int)ErrorCode.WorkflowTaskExecutionsList,
            string.Join(Environment.NewLine, exceptions.Select(e => e.Message))
        );

        throw new FlowSynxException(errorMessage);
    }
}