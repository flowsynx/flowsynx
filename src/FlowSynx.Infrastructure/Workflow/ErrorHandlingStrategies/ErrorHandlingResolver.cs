using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class ErrorHandlingResolver : IErrorHandlingResolver
{
    public void Resolve(WorkflowDefinition definition)
    {
        foreach (var task in definition.Tasks)
        {
            task.ErrorHandling ??= new ErrorHandling();
            task.ErrorHandling = GetEffectiveErrorHandling(task, definition.Configuration);
        }
    }

    private ErrorHandling? GetEffectiveErrorHandling(WorkflowTask task, WorkflowConfiguration config)
    {
        var defaultHandling = config.ErrorHandling;
        var taskHandling = task.ErrorHandling;

        if (defaultHandling is null)
            return null;

        if (taskHandling is null)
            return defaultHandling;

        return new ErrorHandling
        {
            Strategy = taskHandling.Strategy ?? defaultHandling.Strategy,
            RetryPolicy = MergeRetryPolicy(taskHandling.RetryPolicy, defaultHandling.RetryPolicy ?? new RetryPolicy())
        };
    }

    private RetryPolicy MergeRetryPolicy(RetryPolicy? taskPolicy, RetryPolicy defaultPolicy)
    {
        if (taskPolicy is null)
            return defaultPolicy;

        return new RetryPolicy
        {
            MaxRetries = IsValid(taskPolicy.MaxRetries) ? taskPolicy.MaxRetries : defaultPolicy.MaxRetries,
            BackoffStrategy = IsDefined(taskPolicy.BackoffStrategy) ? taskPolicy.BackoffStrategy : defaultPolicy.BackoffStrategy,
            InitialDelay = IsPositive(taskPolicy.InitialDelay) ? taskPolicy.InitialDelay : defaultPolicy.InitialDelay,
            MaxDelay = IsPositive(taskPolicy.MaxDelay) ? taskPolicy.MaxDelay : defaultPolicy.MaxDelay,
            Factor = IsPositive(taskPolicy.Factor) ? taskPolicy.Factor : defaultPolicy.Factor
        };
    }

    private bool IsValid(int value) => value >= 0;
    private bool IsPositive(int value) => value > 0;
    private bool IsPositive(double value) => value > 0.0;
    private bool IsDefined(BackoffStrategy strategy) => Enum.IsDefined(typeof(BackoffStrategy), strategy);
}
