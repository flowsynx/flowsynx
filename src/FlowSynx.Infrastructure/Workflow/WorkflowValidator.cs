using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Models;
using FlowSynx.Application.Workflow;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowValidator : IWorkflowValidator
{
    public void Validate(WorkflowDefinition definition)
    {
        var tasks = definition.Tasks;
        EnsureNoDuplicateTaskNames(tasks);
        EnsureAllDependenciesExist(tasks);
        EnsureNoCyclicDependencies(tasks);
        ValidateRetryPolicies(tasks);
    }

    private void EnsureNoDuplicateTaskNames(IEnumerable<WorkflowTask> tasks)
    {
        if (HasDuplicateNames(tasks))
        {
            throw new FlowSynxException(
                (int)ErrorCode.WorkflowHasDuplicateNames,
                Resources.Workflow_Executor_DuplicatedTasksName);
        }
    }

    private void EnsureAllDependenciesExist(IEnumerable<WorkflowTask> tasks)
    {
        var missingDependencies = AllDependenciesExist(tasks);
        if (missingDependencies.Any())
        {
            var message = string.Format(
                Resources.Workflow_Executor_MissingDependencies,
                string.Join(",", missingDependencies));

            throw new FlowSynxException(
                (int)ErrorCode.WorkflowMissingDependencies,
                message);
        }
    }

    private void EnsureNoCyclicDependencies(IEnumerable<WorkflowTask> tasks)
    {
        var validation = CheckCyclic(tasks);
        if (validation.Cyclic)
        {
            var message = string.Format(
                Resources.Workflow_Executor_CyclicDependencies,
                string.Join(" -> ", validation.CyclicNodes));

            throw new FlowSynxException(
                (int)ErrorCode.WorkflowCyclicDependencies,
                message);
        }
    }

    private void ValidateRetryPolicies(IEnumerable<WorkflowTask> tasks)
    {
        var errors = tasks
            .SelectMany(task =>
            {
                var retry = task.ErrorHandling?.RetryPolicy;
                var taskErrors = new List<string>();

                if (retry?.MaxRetries is < 0)
                    taskErrors.Add(string.Format(Resources.WorkflowValidator_TaskHasNegativeMaxRetries, task.Name, retry.MaxRetries));

                if (retry?.InitialDelay is < 0)
                    taskErrors.Add(string.Format(Resources.WorkflowValidator_TaskHasNegativeInitialDelay, task.Name, retry.InitialDelay));

                if (retry?.MaxDelay is < 0)
                    taskErrors.Add(string.Format(Resources.WorkflowValidator_TaskHasNegativeMaxDelay, task.Name, retry.MaxDelay));

                if (retry?.Factor is < 0)
                    taskErrors.Add(string.Format(Resources.WorkflowValidator_TaskHasNegativeFactor, task.Name, retry.MaxDelay));

                return taskErrors;
            })
            .ToList();

        if (errors.Any())
        {
            throw new Exception(string.Join(Environment.NewLine, errors));
        }
    }

    #region private methods
    private List<string> AllDependenciesExist(IEnumerable<WorkflowTask> workflowTasks)
    {
        var definedTaskNames = workflowTasks.Select(t => t.Name).ToHashSet();
        var missingDependencies = workflowTasks
            .SelectMany(t => t.Dependencies)
            .Where(dep => !definedTaskNames.Contains(dep))
            .Distinct()
            .ToList();

        return missingDependencies;
    }

    private WorkflowValidatorResult CheckCyclic(IEnumerable<WorkflowTask> workflowTasks)
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

    private bool HasDuplicateNames(IEnumerable<WorkflowTask> workflowTasks)
    {
        var seen = new HashSet<string>();
        return workflowTasks.Any(task => !seen.Add(task.Name));
    }

    private Dictionary<string, List<string>> BuildGraph(IEnumerable<WorkflowTask> tasks, out Dictionary<string, int> inDegree)
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
    #endregion
}