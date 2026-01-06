using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Logging;

public class TenantLoggingService : IDisposable
{
    private readonly ISecretProviderFactory _secretProviderFactory;
    private readonly ILogger<TenantLoggingService> _logger;
    private readonly ConcurrentDictionary<TenantId, Logger> _tenantLoggers = new();

    public TenantLoggingService(
        ISecretProviderFactory secretProviderFactory,
        ILogger<TenantLoggingService> logger)
    {
        _secretProviderFactory = secretProviderFactory ?? throw new ArgumentNullException(nameof(secretProviderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ConfigureTenantLoggerAsync(TenantId tenantId)
    {
        if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));

        var provider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
        var secrets = await provider.GetSecretsAsync();
        var loggingPolicy = secrets.GetLoggingPolicy();

        if (loggingPolicy == null) return;

        var logger = CreateLoggerForTenant(tenantId, loggingPolicy);

        if (_tenantLoggers.TryGetValue(tenantId, out var existingLogger))
        {
            existingLogger.Dispose();
        }

        _tenantLoggers[tenantId] = logger;
    }

    private Logger CreateLoggerForTenant(TenantId tenantId, TenantLoggingPolicy policy)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("TenantId", tenantId)
            .MinimumLevel.Is(policy.DefaultLogLevel.GetLogEventLevel());

        ConfigureSinks(loggerConfig, policy);

        return loggerConfig.CreateLogger();
    }

    private void ConfigureSinks(LoggerConfiguration config, TenantLoggingPolicy policy)
    {
        if (policy?.Enabled != true) return;

        ConfigureFileSink(config, policy.File);
        ConfigureSeqSink(config, policy.Seq);
    }

    private void ConfigureFileSink(LoggerConfiguration config, TenantFileLoggingPolicy? filePolicy)
    {
        if (filePolicy == null || string.IsNullOrWhiteSpace(filePolicy.LogPath)) return;

        var logPath = Path.Combine(filePolicy.LogPath, "log-.txt");

        config.WriteTo.File(
            path: logPath,
            restrictedToMinimumLevel: LogLevelMapper.Map(filePolicy.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Tenant:{TenantId}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] " +
                "{Message:lj}{NewLine}{Exception}",
            rollingInterval: filePolicy.RollingInterval.RollingIntervalFromString(),
            retainedFileCountLimit: filePolicy.RetainedFileCountLimit ?? 7,
            shared: true);
    }

    private void ConfigureSeqSink(LoggerConfiguration config, TenantSeqLoggingPolicy? seqPolicy)
    {
        if (seqPolicy == null || string.IsNullOrWhiteSpace(seqPolicy.Url)) return;

        config.WriteTo.Seq(
            serverUrl: seqPolicy.Url!,
            apiKey: string.IsNullOrWhiteSpace(seqPolicy.ApiKey) ? null : seqPolicy.ApiKey,
            restrictedToMinimumLevel: LogLevelMapper.Map(seqPolicy.LogLevel));
    }

    public Logger? GetTenantLogger(TenantId tenantId)
    {
        if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
        return _tenantLoggers.TryGetValue(tenantId, out var logger) ? logger : null;
    }

    public void Dispose()
    {
        foreach (var logger in _tenantLoggers.Values)
        {
            logger.Dispose();
        }

        _tenantLoggers.Clear();
    }
}
