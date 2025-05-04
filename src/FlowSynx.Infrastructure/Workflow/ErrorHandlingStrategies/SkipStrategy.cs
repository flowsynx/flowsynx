using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Workflow.ErrorHandlingStrategies;

public class SkipStrategy: IErrorHandlingStrategy
{
    private readonly ILogger _logger;

    public SkipStrategy(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<ErrorHandlingResult> HandleAsync(
        ErrorHandlingContext context,
        CancellationToken cancellation)
    {
        _logger.LogInformation($"Task '{context.TaskName}' has failed; however, it has been skipped in accordance with the defined error handling strategy.");
        return Task.FromResult(new ErrorHandlingResult { ShouldSkip = true });
    }
}