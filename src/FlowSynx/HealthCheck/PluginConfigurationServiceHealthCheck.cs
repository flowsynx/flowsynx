using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class PluginConfigurationServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<PluginConfigurationServiceHealthCheck> _logger;
    private readonly IPluginConfigurationService _pluginConfigurationService;
    private readonly ILocalization _localization;

    public PluginConfigurationServiceHealthCheck(
        ILogger<PluginConfigurationServiceHealthCheck> logger,
        IPluginConfigurationService pluginConfigurationService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginConfigurationService);
        ArgumentNullException.ThrowIfNull(localization);
        ;
        _logger = logger;
        _pluginConfigurationService = pluginConfigurationService;
        _localization = localization;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _pluginConfigurationService.CheckHealthAsync(cancellationToken);
            if (healthStatus is false)
                throw new FlowSynxException((int)ErrorCode.ApplicationHealthCheck,
                    _localization.Get("ConfigurationServiceHealthCheckConfigurationServiceFailed"));

            return HealthCheckResult.Healthy(_localization.Get("ConfigurationServiceHealthCheckConfigurationServiceAvailable"));
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationHealthCheck, 
                $"Error in checking application health. Error: {ex.Message}");
            _logger.LogError(errorMessage.ToString());
            return HealthCheckResult.Unhealthy(_localization.Get("ConfigurationServiceHealthCheckConfigurationServiceFailed"));
        }
    }
}