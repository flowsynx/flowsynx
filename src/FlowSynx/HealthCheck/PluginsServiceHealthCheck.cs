using FlowSynx.Application.Models;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Interfaces;
using FlowSynx.PluginCore.Exceptions;
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
                throw new FlowSynxException((int)ErrorCode.ApplicationHealthCheck, Resources.PluginServiceHealthCheckPluginServiceFailed);

            return HealthCheckResult.Healthy(Resources.PluginServiceHealthCheckPluginServiceAvailable);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.ApplicationHealthCheck, $"Error in checking plugin service health. Error: {ex.Message}");
            _logger.LogError(errorMessage.ToString());
            return HealthCheckResult.Unhealthy(Resources.PluginServiceHealthCheckPluginServiceFailed);
        }
    }
}