using FlowSynx.Application.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.AI;

public class AgentExecutor : IAgentExecutor
{
    private readonly IAiFactory _aiFactory;
    private readonly ILogger<AgentExecutor> _logger;

    public AgentExecutor(
        IAiFactory aiFactory,
        ILogger<AgentExecutor> logger)
    {
        _aiFactory = aiFactory ?? throw new ArgumentNullException(nameof(aiFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            return config.Mode switch
            {
                "execute" => await provider.ExecuteAgenticTaskAsync(context, config, cancellationToken),
                "plan" => await ExecutePlanModeAsync(provider, context, cancellationToken),
                "validate" => await ExecuteValidateModeAsync(provider, context, cancellationToken),
                "assist" => await provider.ExecuteAgenticTaskAsync(context, config, cancellationToken),
                _ => throw new ArgumentException($"Unknown agent mode: {config.Mode}")
            };
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
        // Agent can assist with any task type
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