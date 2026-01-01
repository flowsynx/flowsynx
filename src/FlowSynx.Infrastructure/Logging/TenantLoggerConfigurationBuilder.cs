using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public sealed class TenantLoggerConfigurationBuilder : ILoggerConfigurationBuilder
{
    private readonly IReadOnlyList<ILoggingSinkConfigurator> _sinkConfigurators;

    public TenantLoggerConfigurationBuilder(IEnumerable<ILoggingSinkConfigurator> sinkConfigurators)
    {
        _sinkConfigurators = sinkConfigurators.ToList();
    }

    public LoggerConfiguration Build(TenantId tenantId, LoggingConfiguration config)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("TenantId", tenantId);

        foreach (var configurator in _sinkConfigurators)
        {
            loggerConfig = configurator.Configure(loggerConfig, tenantId, config);
        }

        return loggerConfig;
    }
}