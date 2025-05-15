using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class AbortStrategy: IErrorHandlingStrategy
{
    private readonly ILogger _logger;

    public AbortStrategy(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context, 
        CancellationToken cancellation)
    {
        _logger.LogInformation(string.Format(Resources.Workflow_AbortStrategy_Handle, context.TaskName));
        return Task.FromResult(new ErrorHandlingResult { ShouldAbortWorkflow = true });
    }
}