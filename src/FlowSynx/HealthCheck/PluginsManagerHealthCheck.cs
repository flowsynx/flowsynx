﻿using EnsureThat;
using FlowSynx.Plugin.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class PluginsManagerHealthCheck : IHealthCheck
{
    private readonly ILogger<PluginsManagerHealthCheck> _logger;
    private readonly IPluginsManager _pluginsManager;

    public PluginsManagerHealthCheck(ILogger<PluginsManagerHealthCheck> logger, IPluginsManager pluginsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(pluginsManager, nameof(pluginsManager));
        _logger = logger;
        _pluginsManager = pluginsManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _pluginsManager.Plugins();
            return Task.FromResult(HealthCheckResult.Healthy(Resources.PluginsManagerHealthCheckConfigurationRegistryAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Plugins manager health checking: Error: {ex.Message}");
            return Task.FromResult(HealthCheckResult.Unhealthy(Resources.PluginsManagerHealthCheckConfigurationRegistryFailed));
        }
    }
}