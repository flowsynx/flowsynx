using FlowSynx.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class LogsServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<LogsServiceHealthCheck> _logger;
    private readonly ILoggerService _loggerService;

    public LogsServiceHealthCheck(ILogger<LogsServiceHealthCheck> logger,
        ILoggerService loggerService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerService);
        _logger = logger;
        _loggerService = loggerService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _loggerService.CheckHealthAsync();
            if (healthStatus is false)
                throw new Exception("Service health is fail.");

            return HealthCheckResult.Healthy(Resources.LoggerServiceHealthCheckLoggerServiceAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logger service health checking: Error: {ex.Message}");
            return HealthCheckResult.Unhealthy(Resources.LoggerServiceHealthCheckLoggerServiceFailed);
        }
    }
}