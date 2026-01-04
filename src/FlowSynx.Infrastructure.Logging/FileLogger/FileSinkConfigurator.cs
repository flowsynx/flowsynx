using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.Extensions;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.FileLogger;

public sealed class FileSinkConfigurator : ILoggingSinkConfigurator
{
    public LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, TenantLoggingPolicy policy)
    {
        var logPolicies = policy.File;

        if (string.IsNullOrWhiteSpace(logPolicies.LogPath))
            return configuration;

        var filePath = logPolicies.LogPath;
        var logPath = Path.Combine(filePath, $"tenant-{tenantId}", "log-.txt");

        return configuration.WriteTo.File(
            path: logPath,
            restrictedToMinimumLevel: LogLevelMapper.Map(logPolicies.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}",
            rollingInterval: logPolicies.RollingInterval.RollingIntervalFromString(),
            retainedFileCountLimit: logPolicies.RetainedFileCountLimit ?? 7,
            shared: true);
    }
}