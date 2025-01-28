using System.Threading.Tasks;

namespace FlowSynx.Core.Features.Workflow.Query;

public class DAGValidatorResult
{
    public bool Cyclic { get; set; }
    public List<string> CyclicNodes { get; set; } = new List<string>();
}

public class DAGValidator
{
    private readonly WorkflowPipelines _workflowPipelines;

    public DAGValidator(WorkflowPipelines workflowPipelines)
    {
        _workflowPipelines = workflowPipelines;
    }

    public List<string> AllDependenciesExist()
    {
        var allNodeNames = _workflowPipelines.Select(n => n.Name).ToHashSet();
        var missingDependencies = new HashSet<string>();

        foreach (var node in _workflowPipelines)
        {
            foreach (var dependency in node.Dependencies)
            {
                if (!allNodeNames.Contains(dependency))
                {
                    missingDependencies.Add(dependency);
                }
            }
        }

        return missingDependencies.ToList();
    }

    public DAGValidatorResult Check()
    {
        var inDegree = new Dictionary<string, int>();
        var graph = new Dictionary<string, List<string>>();

        // Initialize the graph and in-degree dictionary
        foreach (var task in _workflowPipelines)
        {
            if (!graph.ContainsKey(task.Name))
                graph[task.Name] = new List<string>();

            if (!inDegree.ContainsKey(task.Name))
                inDegree[task.Name] = 0;

            foreach (var dep in task.Dependencies)
            {
                if (!graph.ContainsKey(dep))
                    graph[dep] = new List<string>();

                graph[dep].Add(task.Name);
                inDegree[task.Name] = inDegree.GetValueOrDefault(task.Name, 0) + 1;
            }
        }

        // Kahn's Algorithm
        var zeroInDegreeQueue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var executionOrder = new List<string>();
        var visitedCount = 0;

        while (zeroInDegreeQueue.Any())
        {
            var node = zeroInDegreeQueue.Dequeue();
            executionOrder.Add(node);
            visitedCount++;

            foreach (var neighbor in graph[node])
            {
                inDegree[neighbor]--;

                if (inDegree[neighbor] == 0)
                {
                    zeroInDegreeQueue.Enqueue(neighbor);
                }
            }
        }

        if (visitedCount < inDegree.Count)
        {
            var cyclicNodes = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            return new DAGValidatorResult
            {
                Cyclic = true,
                CyclicNodes = cyclicNodes
            };
        }

        return new DAGValidatorResult
        {
            Cyclic = false
        };
    }
}

