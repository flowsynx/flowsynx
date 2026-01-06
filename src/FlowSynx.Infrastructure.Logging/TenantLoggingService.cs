using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FlowSynx.Infrastructure.Logging;

public class TenantLoggingService
{
    private readonly ISecretProviderFactory _secretProviderFactory;
    private readonly ILogger<TenantLoggingService> _logger;
    private readonly Dictionary<string, Serilog.Core.Logger> _tenantLoggers = new();

    public TenantLoggingService(
        ISecretProviderFactory secretProviderFactory,
        ILogger<TenantLoggingService> logger)
    {
        _secretProviderFactory = secretProviderFactory ?? throw new ArgumentNullException(nameof(secretProviderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ConfigureTenantLogger(TenantId tenantId)
    {
        var provider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
        var secrets = await provider.GetSecretsAsync();
        TenantLoggingPolicy parsedLoggingPolicy = secrets.GetLoggingPolicy();

        if (parsedLoggingPolicy == null) return;

        lock (_tenantLoggers)
        {
            if (_tenantLoggers.ContainsKey(tenantId.ToString()))
            {
                _tenantLoggers[tenantId.ToString()].Dispose();
                _tenantLoggers.Remove(tenantId.ToString());
            }

            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("TenantId", tenantId);
                //.MinimumLevel.Is(GetLogEventLevel(parsedLoggingPolicy.DefaultLogLevel));

            // Configure sinks based on tenant config
            ConfigureSinks(loggerConfig, parsedLoggingPolicy, tenantId);

            _tenantLoggers[tenantId.ToString()] = loggerConfig.CreateLogger();
        }
    }

    private void ConfigureSinks(
        LoggerConfiguration config,
        TenantLoggingPolicy loggingPolicy,
        TenantId tenantId)
    {
        if (loggingPolicy != null && loggingPolicy.Enabled == true) 
        {
            var fileLogPolicies = loggingPolicy.File;

            if (!string.IsNullOrWhiteSpace(fileLogPolicies.LogPath))
            {
                var filePath = fileLogPolicies.LogPath;
                var logPath = Path.Combine(filePath, "log-.txt");

                config.WriteTo.File(
                    path: logPath,
                    restrictedToMinimumLevel: LogLevelMapper.Map(fileLogPolicies.LogLevel),
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                        "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
                        "{Message:lj}{NewLine}{Exception}",
                    rollingInterval: fileLogPolicies.RollingInterval.RollingIntervalFromString(),
                    retainedFileCountLimit: fileLogPolicies.RetainedFileCountLimit ?? 7,
                    shared: true);
            }

            var seqLogConfig = loggingPolicy.Seq;
            if (seqLogConfig != null && !string.IsNullOrWhiteSpace(seqLogConfig.Url))
            {
                config.WriteTo.Seq(
                    serverUrl: seqLogConfig.Url!,
                    apiKey: string.IsNullOrWhiteSpace(seqLogConfig.ApiKey) ? null : seqLogConfig.ApiKey,
                    restrictedToMinimumLevel: LogLevelMapper.Map(seqLogConfig.LogLevel));
            }
        }
    }

    private static Serilog.Events.LogEventLevel GetLogEventLevel(string level) => level.ToUpper() switch
    {
        "VERBOSE" => Serilog.Events.LogEventLevel.Verbose,
        "DEBUG" => Serilog.Events.LogEventLevel.Debug,
        "INFORMATION" => Serilog.Events.LogEventLevel.Information,
        "WARNING" => Serilog.Events.LogEventLevel.Warning,
        "ERROR" => Serilog.Events.LogEventLevel.Error,
        "FATAL" => Serilog.Events.LogEventLevel.Fatal,
        _ => Serilog.Events.LogEventLevel.Information
    };

    public Serilog.Core.Logger? GetTenantLogger(TenantId tenantId)
    {
        lock (_tenantLoggers)
        {
            return _tenantLoggers.TryGetValue(tenantId.ToString(), out var logger) ? logger : null;
        }
    }
}