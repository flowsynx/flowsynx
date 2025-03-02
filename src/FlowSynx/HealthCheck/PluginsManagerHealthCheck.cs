using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowSynx.HealthCheck;

public class ConnectorsManagerHealthCheck : IHealthCheck
{
    private readonly ILogger<ConnectorsManagerHealthCheck> _logger;
    //private readonly IConnectorsManager _connectorsManager;

    public ConnectorsManagerHealthCheck(ILogger<ConnectorsManagerHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(HealthCheckResult.Healthy(Resources.ConnectorsManagerHealthCheckConfigurationRegistryAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Connectors manager health checking: Error: {ex.Message}");
            return Task.FromResult(HealthCheckResult.Unhealthy(Resources.ConnectorsManagerHealthCheckConfigurationRegistryFailed));
        }
    }
}