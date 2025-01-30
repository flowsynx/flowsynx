using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;

namespace FlowSynx.Core.Features.Workflow.Query;

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public interface IWorkflowExecutor
{
    Task<Dictionary<string, object?>> ExecuteAsync(WorkflowPipelines workflowPipelines, WorkflowVariables variables, 
        int degreeOfParallelism, CancellationToken cancellationToken);
}

public class WorkflowExecutor: IWorkflowExecutor
{
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly IConnectorParser _connectorParser;
    private readonly ConcurrentDictionary<string, object?> _taskOutputs = new();

    public WorkflowExecutor(ILogger<WorkflowExecutor> logger, IConnectorParser connectorParser)
    {
        _logger = logger;
        _connectorParser = connectorParser;
    }

    public async Task<Dictionary<string, object?>> ExecuteAsync(WorkflowPipelines workflowPipelines, WorkflowVariables variables, 
        int degreeOfParallelism, CancellationToken cancellationToken)
    {
        var taskMap = workflowPipelines.ToDictionary(t => t.Name);
        var pendingTasks = new HashSet<string>(taskMap.Keys);

        while (pendingTasks.Any())
        {
            var readyTasks = pendingTasks
                .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d) && taskMap[d].Status == TaskStatus.Completed))
                .ToList();

            if (!readyTasks.Any())
                throw new InvalidOperationException("There are failed task in dependencies.");

            var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
            await ProcessWithDegreeOfParallelismAsync(executionTasks, degreeOfParallelism, cancellationToken);

            foreach (var taskId in readyTasks)
                pendingTasks.Remove(taskId);
        }

        var outputs = new Dictionary<string, object?>(_taskOutputs);
        return outputs;
    }

    private async Task ProcessWithDegreeOfParallelismAsync(IEnumerable<WorkflowTask> workflowTasks, int degreeOfParallelism, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(degreeOfParallelism);
        var tasks = new List<Task>();

        foreach (var item in workflowTasks)
        {
            // Wait for the semaphore to allow more tasks
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteTaskAsync(item, cancellationToken); // Your task logic
                }
                finally
                {
                    semaphore.Release(); // Release semaphore after task is done
                }
            }, cancellationToken));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
    }

    private async Task ExecuteTaskAsync(WorkflowTask task, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Executing task {task.Name}...");

            task.Status = TaskStatus.Running;
            var connectorContext = _connectorParser.Parse(task.Type);
            var options = task.Options ?? new ConnectorOptions();//.ToConnectorOptions();
            var context = new Context(options, connectorContext.Next);

            var connector = connectorContext.Current;
            var method = connector.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.Contains(task.Process, StringComparison.OrdinalIgnoreCase));

            if (method == null)
                throw new Exception("Method not found!");

            object[] parameters = { context, cancellationToken };
            Task taskToExecute = (Task)method.Invoke(connector, parameters);
            await taskToExecute.ConfigureAwait(false);

            if (taskToExecute.GetType().IsGenericType)
            {
                var response = taskToExecute.GetType().GetProperty("Result").GetValue(taskToExecute);
                _taskOutputs[task.Name] = response;
                task.Status = TaskStatus.Completed;

                _logger.LogInformation($"Task {task.Name} completed.");
            }
        }
        catch (Exception ex) when (ex is TargetInvocationException)
        {
            task.Status = TaskStatus.Failed;
            _taskOutputs[task.Name] = null;
            _logger.LogError($"Task {task.Name} failed: {ex.Message}");
        }
    }
}