//using FlowSynx.Domain.Interfaces;

//namespace FlowSynx.Domain.Entities.Workflow.Models;

//public class WorkflowValidator : IWorkflowValidator
//{
//    public List<string> AllDependenciesExist(WorkflowTasks workflowTasks)
//    {
//        var allNodeNames = workflowTasks.Select(n => n.Name).ToHashSet();
//        var missingDependencies = new HashSet<string>();

//        foreach (var dependency in workflowTasks.SelectMany(node =>
//                     node.Dependencies.Where(dependency => !allNodeNames.Contains(dependency))))
//        {
//            missingDependencies.Add(dependency);
//        }

//        return missingDependencies.ToList();
//    }

//    public WorkflowValidatorResult CheckCyclic(WorkflowTasks workflowTasks)
//    {
//        var inDegree = new Dictionary<string, int>();
//        var graph = new Dictionary<string, List<string>>();

//        // Initialize the graph and in-degree dictionary
//        foreach (var task in workflowTasks)
//        {
//            if (!graph.ContainsKey(task.Name))
//                graph[task.Name] = new List<string>();

//            if (!inDegree.ContainsKey(task.Name))
//                inDegree[task.Name] = 0;

//            foreach (var dep in task.Dependencies)
//            {
//                if (!graph.ContainsKey(dep))
//                    graph[dep] = new List<string>();

//                graph[dep].Add(task.Name);
//                inDegree[task.Name] = inDegree.GetValueOrDefault(task.Name, 0) + 1;
//            }
//        }

//        // Kahn's Algorithm
//        var zeroInDegreeQueue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
//        var executionOrder = new List<string>();
//        var visitedCount = 0;

//        while (zeroInDegreeQueue.Any())
//        {
//            var node = zeroInDegreeQueue.Dequeue();
//            executionOrder.Add(node);
//            visitedCount++;

//            foreach (var neighbor in graph[node])
//            {
//                inDegree[neighbor]--;

//                if (inDegree[neighbor] == 0)
//                {
//                    zeroInDegreeQueue.Enqueue(neighbor);
//                }
//            }
//        }

//        if (visitedCount < inDegree.Count)
//        {
//            var cyclicNodes = inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
//            return new WorkflowValidatorResult
//            {
//                Cyclic = true,
//                CyclicNodes = cyclicNodes
//            };
//        }

//        return new WorkflowValidatorResult
//        {
//            Cyclic = false
//        };
//    }

//    public bool HasDuplicateNames(WorkflowTasks workflowTasks)
//    {
//        var knownKeys = new HashSet<string>();
//        return workflowTasks.Any(item => !knownKeys.Add(item.Name));
//    }
//}

