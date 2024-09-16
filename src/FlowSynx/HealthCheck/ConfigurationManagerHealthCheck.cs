using EnsureThat;
using FlowSynx.Configuration.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class ConfigurationManagerHealthCheck : IHealthCheck
{
    private readonly ILogger<ConfigurationManagerHealthCheck> _logger;
    private readonly FlowSynx.Configuration.IConfigurationManager _configurationManager;

    public ConfigurationManagerHealthCheck(ILogger<ConfigurationManagerHealthCheck> logger, FlowSynx.Configuration.IConfigurationManager configurationManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        _logger = logger;
        _configurationManager = configurationManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var listOptions = new ConfigurationListOptions();
            _configurationManager.List(listOptions);
            return Task.FromResult(HealthCheckResult.Healthy(Resources.ConfigurationManagerHealthCheckConfigurationRegistryAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Configuration manager health checking: Error: {ex.Message}");
            return Task.FromResult(HealthCheckResult.Unhealthy(Resources.ConfigurationManagerHealthCheckConfigurationRegistryFailed));
        }
    }
}