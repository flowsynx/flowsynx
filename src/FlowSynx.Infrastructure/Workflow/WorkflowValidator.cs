using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Workflow;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowValidator : IWorkflowValidator
{
    public List<string> AllDependenciesExist(List<WorkflowTask> workflowTasks)
    {
        var definedTaskNames = workflowTasks.Select(t => t.Name).ToHashSet();
        var missingDependencies = workflowTasks
            .SelectMany(t => t.Dependencies)
            .Where(dep => !definedTaskNames.Contains(dep))
            .Distinct()
            .ToList();

        return missingDependencies;
    }

    public WorkflowValidatorResult CheckCyclic(List<WorkflowTask> workflowTasks)
    {
        var graph = BuildGraph(workflowTasks, out var inDegree);

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var visitedCount = 0;
        var executionOrder = new List<string>();

        while (queue.Any())
        {
            var current = queue.Dequeue();
            executionOrder.Add(current);
            visitedCount++;

            foreach (var neighbor in graph[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (visitedCount < inDegree.Count)
        {
            var cyclicNodes = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            return new WorkflowValidatorResult
            {
                Cyclic = true,
                CyclicNodes = cyclicNodes
            };
        }

        return new WorkflowValidatorResult { Cyclic = false };
    }

    public bool HasDuplicateNames(List<WorkflowTask> workflowTasks)
    {
        var seen = new HashSet<string>();
        return workflowTasks.Any(task => !seen.Add(task.Name));
    }

    private Dictionary<string, List<string>> BuildGraph(List<WorkflowTask> tasks, out Dictionary<string, int> inDegree)
    {
        var graph = new Dictionary<string, List<string>>();
        inDegree = new Dictionary<string, int>();

        foreach (var task in tasks)
        {
            if (!graph.ContainsKey(task.Name))
                graph[task.Name] = new List<string>();

            inDegree.TryAdd(task.Name, 0);

            foreach (var dep in task.Dependencies)
            {
                if (!graph.ContainsKey(dep))
                    graph[dep] = new List<string>();

                graph[dep].Add(task.Name);
                inDegree[task.Name] = inDegree.GetValueOrDefault(task.Name) + 1;
            }
        }

        return graph;
    }
}