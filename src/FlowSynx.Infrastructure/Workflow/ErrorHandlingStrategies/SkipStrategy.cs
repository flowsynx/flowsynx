using FlowSynx.Application.Localizations;
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
        _logger.LogInformation(Localization.Get("Workflow_SkipStrategy_handle", context.TaskName));
        return Task.FromResult(new ErrorHandlingResult { ShouldSkip = true });
    }
}