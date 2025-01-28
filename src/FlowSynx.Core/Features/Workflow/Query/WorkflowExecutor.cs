using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FlowSynx.Core.Features.Workflow.Query;

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public class WorkflowOutput
{
    public string? Description { get; set; }
    public object? Value { get; set; }
}

public class WorkflowExecutor
{
    public readonly WorkflowPipelines _workflowPipelines;
    private Dictionary<string, object> _variables;
    private readonly int _degreeOfParallelism;
    private readonly ConcurrentDictionary<string, object> _taskOutputs = new();
    public Dictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();

    public WorkflowExecutor(WorkflowPipelines workflowPipelines, Dictionary<string, object> variables, int degreeOfParallelism = 3)
    {
        _workflowPipelines = workflowPipelines;
        _variables = variables;
        _degreeOfParallelism = degreeOfParallelism;
    }

    public async Task<Dictionary<string, object>> ExecuteAsync()
    {
        var taskMap = _workflowPipelines.ToDictionary(t => t.Name);
        var pendingTasks = new HashSet<string>(taskMap.Keys);

        while (pendingTasks.Any())
        {
            var readyTasks = pendingTasks
                .Where(t => taskMap[t].Dependencies.All(d => _taskOutputs.ContainsKey(d) && taskMap[d].Status == TaskStatus.Completed))
                .ToList();

            if (!readyTasks.Any())
                throw new InvalidOperationException("There are failed task in dependencies.");

            var executionTasks = readyTasks.Select(taskId => taskMap[taskId]);
            await ProcessWithDegreeOfParallelismAsync(executionTasks, degreeOfParallelism: _degreeOfParallelism);

            //var executionTasks = readyTasks.Select(taskId => ExecuteTaskAsync(taskMap[taskId]));
            //await Task.WhenAll(executionTasks);

            foreach (var taskId in readyTasks)
                pendingTasks.Remove(taskId);
        }

        var outputs = new Dictionary<string, object>(_taskOutputs);
        return outputs;
    }

    private async Task ProcessWithDegreeOfParallelismAsync(IEnumerable<WorkflowTask> workflowTasks, int degreeOfParallelism)
    {
        using var semaphore = new SemaphoreSlim(degreeOfParallelism);
        var tasks = new List<Task>();

        foreach (var item in workflowTasks)
        {
            // Wait for the semaphore to allow more tasks
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteTaskAsync(item); // Your task logic
                }
                finally
                {
                    semaphore.Release(); // Release semaphore after task is done
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
    }

    private IEnumerable<IEnumerable<WorkflowTask>> Partition(IEnumerable<WorkflowTask> source, int size)
    {
        return source
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / size)
            .Select(g => g.Select(x => x.item));
    }

    private async Task ExecuteTaskAsync(WorkflowTask task)
    {
        try
        {
            task.Status = TaskStatus.Running;
            Console.WriteLine($"Executing task {task.Name}...");

            await Task.Delay(500);
            var output = new { Result = $"Output of {task.Name}" };

            _taskOutputs[task.Name] = output;
            task.Status = TaskStatus.Completed;
            Console.WriteLine($"Task {task.Name} completed.");
        }
        catch (Exception ex)
        {
            task.Status = TaskStatus.Failed;
            _taskOutputs[task.Name] = null;
            Console.WriteLine($"Task {task.Name} failed: {ex.Message}");
        }
    }

    //private Dictionary<string, object> ReplaceWorkflowReferences(WorkflowOutputs? outputs)
    //{
    //    var outputsJson = _serializer.Serialize(outputs);
    //    var updatedWorkflowJson = ReplaceReferences(outputsJson);

    //    return DeserializeJsonToDictionary(updatedWorkflowJson);

    //    //return JsonConvert.DeserializeObject<Dictionary<string, object>>(updatedWorkflowJson);
    //}

    //private Dictionary<string, object> DeserializeJsonToDictionary(string json)
    //{
    //    // Deserialize the JSON string into a dictionary
    //    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

    //    // Convert any nested JSON objects into dictionaries
    //    foreach (var key in dictionary.Keys.ToList())
    //    {
    //        // Handle nested objects that may need to be further deserialized
    //        if (dictionary[key] is JObject nestedObject)
    //        {
    //            dictionary[key] = nestedObject.ToObject<Dictionary<string, object>>();
    //        }
    //    }

    //    return dictionary;
    //}
}