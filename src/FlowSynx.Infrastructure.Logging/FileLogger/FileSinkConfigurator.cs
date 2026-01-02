using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;
using FlowSynx.Infrastructure.Logging.Extensions;
using Serilog;

namespace FlowSynx.Infrastructure.Logging.FileLogger;

public sealed class FileSinkConfigurator : ILoggingSinkConfigurator
{
    public LoggerConfiguration Configure(LoggerConfiguration configuration, TenantId tenantId, LoggingConfiguration config)
    {
        var logConfig = config.File;

        if (string.IsNullOrWhiteSpace(logConfig.LogPath))
            return configuration;

        var filePath = logConfig.LogPath;
        var logPath = Path.Combine(filePath, $"tenant-{tenantId}", "log-.txt");

        return configuration.WriteTo.File(
            path: logPath,
            restrictedToMinimumLevel: LogLevelMapper.Map(logConfig.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}",
            rollingInterval: logConfig.RollingInterval.RollingIntervalFromString(),
            retainedFileCountLimit: logConfig.RetainedFileCountLimit ?? 7,
            shared: true);
    }
}