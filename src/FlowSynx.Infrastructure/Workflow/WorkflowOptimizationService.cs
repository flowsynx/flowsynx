using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Workflow;

namespace FlowSynx.Infrastructure.Workflow;

public sealed class WorkflowOptimizationService : IWorkflowOptimizationService
{
    public Task<(WorkflowDefinition Optimized, string Explanation)> OptimizeAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken)
    {
        // Defensive copy
        var copy = new WorkflowDefinition
        {
            Name = definition.Name,
            Description = definition.Description,
            Variables = definition.Variables,
            Configuration = definition.Configuration ?? new WorkflowConfiguration(),
            Tasks = definition.Tasks?.ToList() ?? new List<WorkflowTask>()
        };

        var explanation = new List<string>();

        // 1) Compute structural parallelism width (simple levelization)
        var (_, maxWidth) = ComputeLevels(copy.Tasks);
        if (maxWidth > 0)
        {
            var recommendedDop = Math.Clamp(maxWidth, 2, Environment.ProcessorCount * 2);
            if (copy.Configuration.DegreeOfParallelism is null || copy.Configuration.DegreeOfParallelism != recommendedDop)
            {
                copy.Configuration.DegreeOfParallelism = recommendedDop;
                explanation.Add($"Adjusted DegreeOfParallelism to {recommendedDop} based on computed max DAG width {maxWidth}.");
            }
        }

        // 2) Set conservative workflow timeout if missing (e.g., 30 minutes)
        if (copy.Configuration.Timeout is null)
        {
            copy.Configuration.Timeout = 30 * 60 * 1000;
            explanation.Add("Set workflow timeout to 30m (ms) as a conservative default.");
        }

        // 3) Per-task timeouts: if missing, add modest default (e.g., 2 minutes)
        foreach (var task in copy.Tasks.Where(t => t.Timeout is null))
        {
            task.Timeout = 2 * 60 * 1000;
            explanation.Add($"Task '{task.Name}': applied default timeout 2m (ms).");
        }

        // 4) Normalize dependencies collections
        foreach (var task in copy.Tasks)
        {
            if (task.Dependencies != null)
                task.Dependencies = task.Dependencies.Distinct(StringComparer.Ordinal).ToList();
        }

        // (future) 5) Telemetry-driven tuning hooks can be added here (p95-based timeouts, adaptive retries, etc.)

        var note = explanation.Count == 0 ? "No changes required; workflow already optimized."
                                          : string.Join(" ", explanation);
        return Task.FromResult<(WorkflowDefinition, string)>((copy, note));
    }

    private static (List<List<WorkflowTask>> Levels, int MaxWidth) ComputeLevels(List<WorkflowTask> tasks)
    {
        var levels = new List<List<WorkflowTask>>();
        var nameToTask = tasks.ToDictionary(t => t.Name);
        var remaining = new HashSet<string>(nameToTask.Keys, StringComparer.Ordinal);

        var depsMap = tasks.ToDictionary(
            t => t.Name,
            t => (t.Dependencies ?? new List<string>()).Where(d => nameToTask.ContainsKey(d)).ToHashSet(StringComparer.Ordinal));

        while (remaining.Count > 0)
        {
            var layer = remaining.Where(n => depsMap[n].Count == 0).Select(n => nameToTask[n]).ToList();
            if (layer.Count == 0) break; // cycle or unresolved

            levels.Add(layer);

            foreach (var t in layer)
                remaining.Remove(t.Name);

            foreach (var kv in depsMap)
            {
                foreach (var l in layer)
                    kv.Value.Remove(l.Name);
            }
        }

        var maxWidth = levels.Count == 0 ? 0 : levels.Max(l => l.Count);
        return (levels, maxWidth);
    }
}