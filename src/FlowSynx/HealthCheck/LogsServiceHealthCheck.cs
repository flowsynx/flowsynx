using FlowSynx.Application;
using FlowSynx.Domain.Primitives;
using FlowSynx.Localization;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class LogsServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<LogsServiceHealthCheck> _logger;
    private readonly ILogEntryRepository _logEntryRepository;

    public LogsServiceHealthCheck(ILogger<LogsServiceHealthCheck> logger,
        ILogEntryRepository logEntryRepository)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(logEntryRepository);
        _logger = logger;
        _logEntryRepository = logEntryRepository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _logEntryRepository.CheckHealthAsync();
            if (healthStatus is false)
                throw new FlowSynxException((int)ErrorCode.ApplicationHealthCheck, FlowSynxResources.LoggerServiceHealthCheckFailed);

            return HealthCheckResult.Healthy(FlowSynxResources.LoggerServiceHealthCheckLoggerServiceAvailable);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationHealthCheck, 
                $"Error in checking logger service health. Error: {ex.Message}");
            _logger.LogError(errorMessage.ToString());
            return HealthCheckResult.Unhealthy(FlowSynxResources.LoggerServiceHealthCheckFailed);
        }
    }
}