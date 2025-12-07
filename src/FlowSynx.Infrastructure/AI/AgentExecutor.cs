using FlowSynx.Application.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.AI;

public class AgentExecutor : IAgentExecutor
{
    private readonly IAiFactory _aiFactory;
    private readonly ILogger<AgentExecutor> _logger;
    private readonly IAgentToolRegistry? _toolRegistry;

    public AgentExecutor(
        IAiFactory aiFactory,
        ILogger<AgentExecutor> logger,
        IAgentToolRegistry? toolRegistry = null)
    {
        _aiFactory = aiFactory ?? throw new ArgumentNullException(nameof(aiFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toolRegistry = toolRegistry;
    }

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionContext context,
        AgentConfiguration config,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Agent executing task '{TaskName}' with mode '{Mode}'",
            context.TaskName,
            config.Mode);

        try
        {
            var provider = _aiFactory.GetDefaultProvider();

            // If tool selection is none or registry missing, fall back to provider behavior
            var useToolLoop = _toolRegistry is not null && (config.ToolSelection is null || !string.Equals(config.ToolSelection, "none", StringComparison.OrdinalIgnoreCase));

            if (!useToolLoop || config.Mode is "plan" or "validate")
            {
                return config.Mode switch
                {
                    "execute" => await provider.ExecuteAgenticTaskAsync(context, config, cancellationToken),
                    "plan" => await ExecutePlanModeAsync(provider, context, cancellationToken),
                    "validate" => await ExecuteValidateModeAsync(provider, context, cancellationToken),
                    "assist" => await provider.ExecuteAgenticTaskAsync(context, config, cancellationToken),
                    _ => throw new ArgumentException($"Unknown agent mode: {config.Mode}")
                };
            }

            // Tools-first loop for execute/assist
            var result = new AgentExecutionResult
            {
                Success = true,
                Reasoning = "Action-Observation loop executed",
            };

            var tools = _toolRegistry!.GetAllowedTools(config.AllowTools, config.DenyTools).ToList();
            if (tools.Count == 0)
            {
                _logger.LogInformation("No allowed tools found for agent; deferring to provider.");
                return await provider.ExecuteAgenticTaskAsync(context, config, cancellationToken);
            }

            var maxIterations = Math.Max(1, config.MaxIterations);
            var maxToolCalls = Math.Max(1, config.MaxToolCalls);
            var toolCalls = 0;

            // Simple heuristic: pick tool by name == TaskType (string) or first allowed
            var taskTypeName = context.TaskType?.ToString();
            var selectedTool = tools.FirstOrDefault(t => string.Equals(t.Name, taskTypeName, StringComparison.OrdinalIgnoreCase))
                               ?? tools.First();

            for (var i = 0; i < maxIterations && toolCalls < maxToolCalls; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step = new AgentStep
                {
                    Thought = $"Iteration {i + 1}: selecting tool '{selectedTool.Name}'",
                    Action = selectedTool.Name,
                    Args = new Dictionary<string, object?>(context.TaskParameters ?? new(), StringComparer.OrdinalIgnoreCase)
                };

                if (config.DryRun)
                {
                    step.Args["dryRun"] = true;
                }

                var observation = await selectedTool.ExecuteAsync(step.Operation, step.Args, cancellationToken);
                step.Observation = observation.Success
                    ? observation.Output
                    : new { error = observation.ErrorMessage ?? "Unknown tool error" };

                result.Trace.Add(step);
                result.Steps.Add($"{selectedTool.Name} called.");

                toolCalls++;

                // Break condition: in assist mode, single call is sufficient to plan args
                if (config.Mode == "assist") break;

                // For execute mode: basic stop when we have a successful observation
                if (observation.Success) break;
            }

            // Output: last observation
            var lastObs = result.Trace.LastOrDefault()?.Observation;
            result.Output = lastObs;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed for task '{TaskName}'", context.TaskName);
            return new AgentExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Steps = new List<string> { $"Error: {ex.Message}" }
            };
        }
    }

    public Task<bool> CanHandleAsync(string taskType, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    private async Task<AgentExecutionResult> ExecutePlanModeAsync(
        IAiProvider provider,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var plan = await provider.PlanTaskExecutionAsync(context, cancellationToken);
        return new AgentExecutionResult
        {
            Success = true,
            Output = plan,
            Reasoning = "Generated execution plan",
            Steps = new List<string> { "Planning completed" }
        };
    }

    private async Task<AgentExecutionResult> ExecuteValidateModeAsync(
        IAiProvider provider,
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var (isValid, message) = await provider.ValidateTaskAsync(context, context.PreviousTaskOutputs, cancellationToken);
        return new AgentExecutionResult
        {
            Success = isValid,
            Output = new { isValid, message },
            Reasoning = message ?? "Validation completed",
            Steps = new List<string> { isValid ? "Validation passed" : "Validation failed" }
        };
    }
}