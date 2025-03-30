using FlowSynx.Application.Models;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class PluginConfigurationServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<PluginConfigurationServiceHealthCheck> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;

    public PluginConfigurationServiceHealthCheck(ILogger<PluginConfigurationServiceHealthCheck> logger,
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
                throw new FlowSynxException((int)ErrorCode.ApplicationHealthCheck, Resources.ConfigurationServiceHealthCheckConfigurationServiceFailed);

            return HealthCheckResult.Healthy(Resources.ConfigurationServiceHealthCheckConfigurationServiceAvailable);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationHealthCheck, $"Error in checking application health. Error: {ex.Message}");
            _logger.LogError(errorMessage.ToString());
            return HealthCheckResult.Unhealthy(Resources.ConfigurationServiceHealthCheckConfigurationServiceFailed);
        }
    }
}