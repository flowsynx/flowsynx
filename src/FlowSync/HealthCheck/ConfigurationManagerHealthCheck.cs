using EnsureThat;
using FlowSync.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSync.HealthCheck;

public class ConfigurationManagerHealthCheck : IHealthCheck
{
    private readonly ILogger<ConfigurationManagerHealthCheck> _logger;
    private readonly IConfigurationManager _configurationManager;

    public ConfigurationManagerHealthCheck(ILogger<ConfigurationManagerHealthCheck> logger, IConfigurationManager configurationManager)
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
            _configurationManager.GetSettings();
            return Task.FromResult(HealthCheckResult.Healthy("Configuration registry available"));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Configuration manager health checking: Error: {ex.Message}");
            return Task.FromResult(HealthCheckResult.Unhealthy("Configuration registry failed"));
        }
    }
}