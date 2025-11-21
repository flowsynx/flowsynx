using FlowSynx.Application.Localizations;
using FlowSynx.Domain;
using FlowSynx.Domain.Plugin;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class PluginsServiceHealthCheck : IHealthCheck
{
    private readonly ILogger<PluginsServiceHealthCheck> _logger;
    private readonly IPluginService _pluginService;
    private readonly ILocalization _localization;

    public PluginsServiceHealthCheck(
        ILogger<PluginsServiceHealthCheck> logger, 
        IPluginService pluginService,
        ILocalization localization)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(pluginService);
        ArgumentNullException.ThrowIfNull(localization);
        _logger = logger;
        _pluginService = pluginService;
        _localization = localization;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _pluginService.CheckHealthAsync(cancellationToken);
            if (healthStatus is false)
                throw new FlowSynxException((int)ErrorCode.ApplicationHealthCheck,
                    _localization.Get("PluginServiceHealthCheckPluginServiceFailed"));

            return HealthCheckResult.Healthy(_localization.Get("PluginServiceHealthCheckPluginServiceAvailable"));
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationHealthCheck, 
                $"Error in checking plugin service health. Error: {ex.Message}");
            _logger.LogError(errorMessage.ToString());
            return HealthCheckResult.Unhealthy(_localization.Get("PluginServiceHealthCheckPluginServiceFailed"));
        }
    }
}