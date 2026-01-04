using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.SeqLogger;

public sealed class SeqSinkConfigurator : ILoggingSinkConfigurator
{
    public LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, TenantLoggingPolicy policy)
    {
        var logConfig = policy.Seq;

        // Expecting something like: Logging.Seq.Url and Logging.Seq.ApiKey
        if (logConfig is null ||
            string.IsNullOrWhiteSpace(logConfig.Url))
        {
            return configuration;
        }

        // Serilog.Sinks.Seq package required
        return configuration.WriteTo.Seq(
            serverUrl: logConfig.Url!,
            apiKey: string.IsNullOrWhiteSpace(logConfig.ApiKey) ? null : logConfig.ApiKey,
            restrictedToMinimumLevel: LogLevelMapper.Map(logConfig.LogLevel));
    }
}