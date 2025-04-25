using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class AbortStrategy: IErrorHandlingStrategy
{
    private readonly ILogger _logger;

    public AbortStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context, 
        CancellationToken cancellation)
    {
        _logger.LogInformation($"Task '{context.TaskName}' has failed; the workflow will be aborted in accordance with the defined error handling strategy.");
        return Task.FromResult(new ErrorHandlingResult { ShouldAbortWorkflow = true });
    }
}