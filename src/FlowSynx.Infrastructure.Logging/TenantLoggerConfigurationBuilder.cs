using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public sealed class TenantLoggerConfigurationBuilder : ILoggerConfigurationBuilder
{
    private readonly IReadOnlyList<ILoggingSinkConfigurator> _sinkConfigurators;

    public TenantLoggerConfigurationBuilder(IEnumerable<ILoggingSinkConfigurator> sinkConfigurators)
    {
        _sinkConfigurators = sinkConfigurators.ToList();
    }

    public LoggerConfiguration Build(TenantId tenantId, TenantLoggingPolicy policy)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("TenantId", tenantId);

        foreach (var configurator in _sinkConfigurators)
        {
            loggerConfig = configurator.Configure(loggerConfig, tenantId, policy);
        }

        return loggerConfig;
    }
}