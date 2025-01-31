using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using FlowSynx.Core.Parers.Connector;
using FlowSynx.Connectors.Abstractions;
using System.Threading.Tasks;

namespace FlowSynx.Core.Features.Workflow.Query;

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

    public async Task<Dictionary<string, object?>> ExecuteAsync(WorkflowExecutionDefinition executionDefinition, 
        CancellationToken cancellationToken)
    {
        var taskMap = executionDefinition.WorkflowPipelines.ToDictionary(t => t.Name);
        var pendingTasks = new HashSet<string>(taskMap.Keys);

        while (pendingTasks.Any())
        {
            var readyTasks = pendingTasks
                .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d) && taskMap[d].Status == WorkflowTaskStatus.Completed))
                .ToList();

            if (!readyTasks.Any())
                throw new InvalidOperationException("There are failed task in dependencies.");

            var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
            await ProcessWithDegreeOfParallelismAsync(executionTasks, executionDefinition.DegreeOfParallelism, cancellationToken);

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
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteTaskAsync(item, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteTaskAsync(WorkflowTask task, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Executing task {task.Name}...");

            task.Status = WorkflowTaskStatus.Running;
            var connectorContext = _connectorParser.Parse(task.Type);
            var options = task.Options ?? new ConnectorOptions();
            var context = new Context(options, connectorContext.Next);

            var connector = connectorContext.Current;
            object[] parameters = { context, cancellationToken };
            var executionResult = await TryExecute(connector, task.Process, parameters);

            if (executionResult != null)
            {
                _taskOutputs[task.Name] = executionResult;
                task.Status = WorkflowTaskStatus.Completed;
                _logger.LogInformation($"Task {task.Name} completed.");
            }
        }
        catch (Exception ex) when (ex is TargetInvocationException)
        {
            task.Status = WorkflowTaskStatus.Failed;
            _taskOutputs[task.Name] = null;
            _logger.LogError($"Task {task.Name} failed: {ex.Message}");
        }
    }

    private async Task<object?> TryExecute(object instance, string methodName, object[] parameters)
    {
        var method = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name.Contains(methodName, StringComparison.OrdinalIgnoreCase));

        if (method == null)
            throw new Exception("Method not found!");

        var taskToExecute = (Task)method?.Invoke(instance, parameters)!;
        await taskToExecute.ConfigureAwait(false);

        if (!taskToExecute.GetType().IsGenericType) 
            return null;

        var response = taskToExecute.GetType().GetProperty("Result")?.GetValue(taskToExecute);
        return response;

    }
}