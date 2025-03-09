//using System.Collections.Concurrent;
//using System.Reflection;
//using Microsoft.Extensions.Logging;
//using FlowSynx.Core.Services;
//using FlowSynx.PluginCore;

//namespace FlowSynx.Core.Features.Workflow;

//public class WorkflowExecutor : IWorkflowExecutor
//{
//    private readonly ILogger<WorkflowExecutor> _logger;
//    private readonly IPluginTypeService _pluginTypeService;
//    private readonly ConcurrentDictionary<string, object?> _taskOutputs = new();

//    public WorkflowExecutor(ILogger<WorkflowExecutor> logger, IPluginTypeService pluginTypeService)
//    {
//        _logger = logger;
//        _pluginTypeService = pluginTypeService;
//    }

//    public async Task<Dictionary<string, object?>> ExecuteAsync(WorkflowExecutionDefinition executionDefinition,
//        CancellationToken cancellationToken)
//    {
//        var taskMap = executionDefinition.WorkflowPipelines.ToDictionary(t => t.Name);
//        var pendingTasks = new HashSet<string>(taskMap.Keys);

//        while (pendingTasks.Any())
//        {
//            var readyTasks = pendingTasks
//                .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d) && taskMap[d].Status == WorkflowTaskStatus.Completed))
//                .ToList();

//            if (!readyTasks.Any())
//                throw new InvalidOperationException("There are failed task in dependencies.");

//            var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
//            await ProcessWithDegreeOfParallelismAsync(executionTasks, executionDefinition.Configuration.DegreeOfParallelism, cancellationToken);

//            foreach (var taskId in readyTasks)
//                pendingTasks.Remove(taskId);
//        }

//        var outputs = new Dictionary<string, object?>(_taskOutputs);
//        return outputs;
//    }

//    private async Task ProcessWithDegreeOfParallelismAsync(IEnumerable<WorkflowTask> workflowTasks, int degreeOfParallelism, CancellationToken cancellationToken)
//    {
//        using var semaphore = new SemaphoreSlim(degreeOfParallelism);
//        var tasks = new List<Task>();

//        foreach (var item in workflowTasks)
//        {
//            await semaphore.WaitAsync(cancellationToken);

//            tasks.Add(Task.Run(async () =>
//            {
//                try
//                {
//                    await ExecuteTaskAsync(item, cancellationToken);
//                }
//                finally
//                {
//                    semaphore.Release();
//                }
//            }, cancellationToken));
//        }

//        await Task.WhenAll(tasks);
//    }

//    private async Task ExecuteTaskAsync(WorkflowTask task, CancellationToken cancellationToken)
//    {
//        try
//        {
//            _logger.LogInformation($"Executing task {task.Name}...");

//            task.Status = WorkflowTaskStatus.Running;
//            var connector = await _pluginTypeService.Get("", task.Type);
//            var options = task.Options ?? new PluginParameters();

//            List<object?> results = new();
//            foreach (var depencency in task.Dependencies)
//            {
//                if (_taskOutputs.TryGetValue(depencency, out var output))
//                {
//                    results.Add(output);
//                }
//            }

//            var context = new Context(options, results);

//            object[] parameters = { context, cancellationToken };
//            var executionResult = await TryExecute(connector, task.Process, parameters);

//            if (executionResult != null)
//            {
//                _taskOutputs[task.Name] = executionResult;
//                task.Status = WorkflowTaskStatus.Completed;
//                _logger.LogInformation($"Task {task.Name} completed.");
//            }
//        }
//        catch (Exception ex) when (ex is TargetInvocationException)
//        {
//            task.Status = WorkflowTaskStatus.Failed;
//            _taskOutputs[task.Name] = null;
//            _logger.LogError($"Task {task.Name} failed: {ex.Message}");
//        }
//    }

//    private async Task<object?> TryExecute(object instance, string methodName, object[] parameters)
//    {
//        var method = instance.GetType()
//            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
//            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

//        if (method == null)
//            throw new Exception("Method not found!");

//        var taskToExecute = (Task)method?.Invoke(instance, parameters)!;
//        await taskToExecute.ConfigureAwait(false);

//        if (!taskToExecute.GetType().IsGenericType)
//            return null;

//        var response = taskToExecute.GetType().GetProperty("Result")?.GetValue(taskToExecute);
//        return response;
//    }
//}