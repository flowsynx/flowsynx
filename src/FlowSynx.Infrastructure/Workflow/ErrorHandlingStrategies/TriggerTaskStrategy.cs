using FlowSynx.Application.Localizations;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class TriggerTaskStrategy : IErrorHandlingStrategy
{
    private readonly string _taskNameToTrigger;
    private readonly ILogger _logger;

    public TriggerTaskStrategy(string taskNameToTrigger, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskNameToTrigger);

        _taskNameToTrigger = taskNameToTrigger;
        _logger = logger;
    }

    public Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            Localization.Get("Workflow_TriggerTaskStrategy_Handle", context.TaskName, _taskNameToTrigger));

        return Task.FromResult(new ErrorHandlingResult
        {
            ShouldRetry = false,
            ShouldSkip = false,
            ShouldAbortWorkflow = false,
            ShouldTriggerTask = true,
            TaskToTrigger = _taskNameToTrigger
        });
    }
}
