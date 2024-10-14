using EnsureThat;
using FlowSynx.Connectors.Manager;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class ConnectorsManagerHealthCheck : IHealthCheck
{
    private readonly ILogger<ConnectorsManagerHealthCheck> _logger;
    private readonly IConnectorsManager _connectorsManager;

    public ConnectorsManagerHealthCheck(ILogger<ConnectorsManagerHealthCheck> logger, IConnectorsManager connectorsManager)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(connectorsManager, nameof(connectorsManager));
        _logger = logger;
        _connectorsManager = connectorsManager;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var listOptions = new ConnectorListOptions();
            _connectorsManager.List(listOptions);
            return Task.FromResult(HealthCheckResult.Healthy(Resources.ConnectorsManagerHealthCheckConfigurationRegistryAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Connectors manager health checking: Error: {ex.Message}");
            return Task.FromResult(HealthCheckResult.Unhealthy(Resources.ConnectorsManagerHealthCheckConfigurationRegistryFailed));
        }
    }
}