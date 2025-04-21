using FlowSynx.Application.Features.Workflows.Command.Execute;

namespace FlowSynx.Infrastructure.Workflow;

public class RetryPolicyApplier: IRetryPolicyApplier
{
    public void Apply(WorkflowDefinition definition)
    {
        foreach (var task in definition.Tasks)
        {
            var getRetryPolicy = GetRetryPolicy(task, definition.Configuration);
            task.RetryPolicy = getRetryPolicy;
        }
    }

    private RetryPolicy? GetRetryPolicy(WorkflowTask task, WorkflowConfiguration workflowConfiguration)
    {
        if (workflowConfiguration.RetryPolicy is null)
            return null;

        if (task.RetryPolicy is null)
            return workflowConfiguration.RetryPolicy;

        // Merge with default for missing fields (optional, for partial overrides)
        return new RetryPolicy
        {
            MaxRetries = task.RetryPolicy.MaxRetries > 0
                ? task.RetryPolicy.MaxRetries
                : workflowConfiguration.RetryPolicy.MaxRetries,

            BackoffStrategy = Enum.IsDefined(typeof(BackoffStrategy), task.RetryPolicy.BackoffStrategy)
                ? task.RetryPolicy.BackoffStrategy
                : workflowConfiguration.RetryPolicy.BackoffStrategy,

            InitialDelay = task.RetryPolicy.InitialDelay > 0
                ? task.RetryPolicy.InitialDelay
                : workflowConfiguration.RetryPolicy.InitialDelay,

            MaxDelay = task.RetryPolicy.MaxDelay > 0
                ? task.RetryPolicy.MaxDelay
                : workflowConfiguration.RetryPolicy.MaxDelay
        };
    }
}