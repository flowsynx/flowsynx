using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Application.Workflow;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Infrastructure.Workflow;

public class WorkflowValidator : IWorkflowValidator
{
    private readonly ILocalization _localization;
    private readonly IExpressionParserFactory _parserFactory;
    private readonly IPlaceholderReplacer _placeholderReplacer;

    public WorkflowValidator(
        ILocalization localization, 
        IExpressionParserFactory parserFactory, 
        IPlaceholderReplacer placeholderReplacer)
    {
        _localization = localization;
        _parserFactory = parserFactory;
        _placeholderReplacer = placeholderReplacer;
    }

    public void Validate(WorkflowDefinition definition)
    {
        var tasks = definition.Tasks;

        var parser = _parserFactory.CreateParser(new Dictionary<string, object?>(), definition.Variables);

        foreach (var task in tasks)
            ParseWorkflowTaskPlaceholders(task, parser);
        
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
                _localization.Get("Workflow_Executor_DuplicatedTasksName"));
        }
    }

    private void EnsureAllDependenciesExist(List<WorkflowTask> tasks)
    {
        var missingDependencies = AllDependenciesExist(tasks);
        if (!missingDependencies.Any()) 
            return;

        var message = string.Format(
            _localization.Get("Workflow_Executor_MissingDependencies",
            string.Join(",", missingDependencies)));

        throw new FlowSynxException(
            (int)ErrorCode.WorkflowMissingDependencies,
            message);
    }

    private void EnsureNoCyclicDependencies(IEnumerable<WorkflowTask> tasks)
    {
        var validation = CheckCyclic(tasks);
        if (!validation.Cyclic) 
            return;

        var message = string.Format(
            _localization.Get("Workflow_Executor_CyclicDependencies",
            string.Join(" -> ", validation.CyclicNodes)));

        throw new FlowSynxException(
            (int)ErrorCode.WorkflowCyclicDependencies,
            message);
    }

    private void ValidateRetryPolicies(IEnumerable<WorkflowTask> tasks)
    {
        var errors = tasks
            .SelectMany(task =>
            {
                var retry = task.ErrorHandling?.RetryPolicy;
                var taskErrors = new List<string>();

                if (retry?.MaxRetries is < 0)
                    taskErrors.Add(_localization.Get("WorkflowValidator_TaskHasNegativeMaxRetries", task.Name, retry.MaxRetries));

                if (retry?.InitialDelay is < 0)
                    taskErrors.Add(_localization.Get("WorkflowValidator_TaskHasNegativeInitialDelay", task.Name, retry.InitialDelay));

                if (retry?.MaxDelay is < 0)
                    taskErrors.Add(_localization.Get("WorkflowValidator_TaskHasNegativeMaxDelay", task.Name, retry.MaxDelay));

                if (retry?.BackoffCoefficient is < 0)
                    taskErrors.Add(_localization.Get("WorkflowValidator_TaskHasNegativeFactor", task.Name, retry.MaxDelay));

                return taskErrors;
            })
            .ToList();

        if (errors.Any())
        {
            throw new Exception(string.Join(Environment.NewLine, errors));
        }
    }

    #region private methods
    private List<string> AllDependenciesExist(List<WorkflowTask> workflowTasks)
    {
        var definedTaskNames = workflowTasks.Select(t => t.Name).ToHashSet();

        var missingDependencies = workflowTasks
            .SelectMany(t => t.Dependencies)
            .Where(dep => !definedTaskNames.Contains(dep))
            .Distinct()
            .ToList();

        var missingBranchTargets = workflowTasks
            .SelectMany(t => t.ConditionalBranches?.Select(b => b.TargetTaskName) ?? [])
            .Where(target => !definedTaskNames.Contains(target))
            .Distinct()
            .ToList();

        return missingDependencies.Concat(missingBranchTargets).Distinct().ToList();
    }

    private WorkflowValidatorResult CheckCyclic(IEnumerable<WorkflowTask> workflowTasks)
    {
        var graph = BuildGraph(workflowTasks, out var inDegree);

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var visitedCount = 0;

        while (queue.Any())
        {
            var current = queue.Dequeue();
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

        if (visitedCount >= inDegree.Count)
        {
            // No cycle
            return new WorkflowValidatorResult { Cyclic = false };
        }
        else
        {
            // There is a cycle
            var cyclicNodes = inDegree
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key)
                .ToList();

            return new WorkflowValidatorResult
            {
                Cyclic = true,
                CyclicNodes = cyclicNodes
            };
        }
    }

    private bool HasDuplicateNames(IEnumerable<WorkflowTask> workflowTasks)
    {
        var seen = new HashSet<string>();
        return workflowTasks.Any(task => !seen.Add(task.Name));
    }

    /// <summary>
    /// Builds an adjacency list representing workflow dependencies and conditional branches.
    /// </summary>
    private Dictionary<string, List<string>> BuildGraph(IEnumerable<WorkflowTask> tasks, out Dictionary<string, int> inDegree)
    {
        var graph = new Dictionary<string, List<string>>();
        inDegree = new Dictionary<string, int>();

        foreach (var task in tasks)
        {
            EnsureNodeExists(graph, inDegree, task.Name);

            AddDependencies(task, graph, inDegree);
            AddConditionalBranches(task, graph, inDegree);
        }

        return graph;
    }

    /// <summary>
    /// Ensures a node exists in both the adjacency list and in-degree map.
    /// </summary>
    private static void EnsureNodeExists(
        Dictionary<string, List<string>> graph,
        Dictionary<string, int> inDegree,
        string node)
    {
        if (!graph.ContainsKey(node))
            graph[node] = new List<string>();
        inDegree.TryAdd(node, 0);
    }

    /// <summary>
    /// Adds a directed edge and updates the in-degree for the target node.
    /// </summary>
    private static void AddEdge(
        Dictionary<string, List<string>> graph,
        Dictionary<string, int> inDegree,
        string source,
        string target)
    {
        EnsureNodeExists(graph, inDegree, source);
        EnsureNodeExists(graph, inDegree, target);

        graph[source].Add(target);
        inDegree[target] = inDegree.GetValueOrDefault(target) + 1;
    }

    /// <summary>
    /// Registers standard dependency edges (dependency -> task).
    /// </summary>
    private static void AddDependencies(
        WorkflowTask task,
        Dictionary<string, List<string>> graph,
        Dictionary<string, int> inDegree)
    {
        foreach (var dep in task.Dependencies)
        {
            AddEdge(graph, inDegree, dep, task.Name);
        }
    }

    /// <summary>
    /// Registers conditional branch edges (task -> conditional target).
    /// </summary>
    private static void AddConditionalBranches(
        WorkflowTask task,
        Dictionary<string, List<string>> graph,
        Dictionary<string, int> inDegree)
    {
        if (task.ConditionalBranches is not { Count: > 0 })
            return;

        foreach (var branch in task.ConditionalBranches.Select(branch => branch.TargetTaskName))
        {
            AddEdge(graph, inDegree, task.Name, branch);
        }
    }

    public void ParseWorkflowTaskPlaceholders(WorkflowTask task, IExpressionParser parser)
    {
        if (task == null) return;

        // Top-level string properties
        task.Name = ReplaceIfNotNull(task.Name, parser);
        task.Description = ReplaceIfNotNull(task.Description, parser);
        task.Output = ReplaceIfNotNull(task.Output, parser);

        // Dependencies
        if (task.Dependencies is { Count: > 0 })
        {
            for (int i = 0; i < task.Dependencies.Count; i++)
            {
                task.Dependencies[i] = ReplaceIfNotNull(task.Dependencies[i], parser);
            }
        }
    }

    private string? ReplaceIfNotNull(string? value, IExpressionParser parser)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : _placeholderReplacer.ReplacePlaceholders(value, parser);
    }
    #endregion
}
