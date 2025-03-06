using FlowSynx.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class ConfigurationServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<ConfigurationServiceHealthCheck> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;

    public ConfigurationServiceHealthCheck(ILogger<ConfigurationServiceHealthCheck> logger,
        IPluginConfigurationService pluginConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _pluginConfigurationService.CheckHealthAsync(cancellationToken);
            if (healthStatus is false)
                throw new Exception("Service health is fail.");

            return HealthCheckResult.Healthy(Resources.ConfigurationServiceHealthCheckConfigurationServiceAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Plugin configuration service health checking: Error: {ex.Message}");
            return HealthCheckResult.Unhealthy(Resources.ConfigurationServiceHealthCheckConfigurationServiceFailed);
        }
    }
}