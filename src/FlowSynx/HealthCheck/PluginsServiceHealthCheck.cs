using FlowSynx.Application.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class PluginsServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<PluginsServiceHealthCheck> _logger;
    private readonly IPluginService _pluginService;

    public PluginsServiceHealthCheck(ILogger<PluginsServiceHealthCheck> logger, IPluginService pluginService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        _logger = logger;
        _pluginService = pluginService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _pluginService.CheckHealthAsync(cancellationToken);
            if (healthStatus is false)
                throw new Exception("Service health is fail.");

            return HealthCheckResult.Healthy(Resources.PluginServiceHealthCheckPluginServiceAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Plugin service health checking: Error: {ex.Message}");
            return HealthCheckResult.Unhealthy(Resources.PluginServiceHealthCheckPluginServiceFailed);
        }
    }
}