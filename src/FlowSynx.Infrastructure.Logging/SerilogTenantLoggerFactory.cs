using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.ConsoleLogger;
using FlowSynx.Infrastructure.Logging.FileLogger;
using FlowSynx.Infrastructure.Logging.SeqLogger;
using Serilog;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Logging;

public sealed class SerilogTenantLoggerFactory : ITenantLoggerFactory
{
    private readonly ISecretProviderFactory _secretProviderFactory;
    private readonly ILoggerConfigurationBuilder _loggerConfigurationBuilder;
    private readonly ConcurrentDictionary<TenantId, CachedLogger> _cache = new();

    public SerilogTenantLoggerFactory(ISecretProviderFactory secretProviderFactory)
        : this(secretProviderFactory, new TenantLoggerConfigurationBuilder(
            new ILoggingSinkConfigurator[]
            {
                new ConsoleSinkConfigurator(),
                new FileSinkConfigurator(),
                new SeqSinkConfigurator()
            }))
    {
    }

    internal SerilogTenantLoggerFactory(
        ISecretProviderFactory secretProviderFactory,
        ILoggerConfigurationBuilder loggerConfigurationBuilder)
    {
        _secretProviderFactory = secretProviderFactory;
        _loggerConfigurationBuilder = loggerConfigurationBuilder;
    }

    public ILogger GetLogger(TenantId tenantId)
    {
        var cached = _cache.GetOrAdd(tenantId, CreateLoggerAsync(tenantId).GetAwaiter().GetResult());
        return cached.Logger;
    }

    private async Task<CachedLogger> CreateLoggerAsync(TenantId tenantId)
    {
        var provider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
        var secrets = await provider.GetSecretsAsync();
        TenantLoggingPolicy parsedLoggingPolicy = ParseLoggingPolicy(secrets);

        // Always allow console logging. Enable file/seq only when tenant is valid.
        ILoggerConfigurationBuilder builderToUse;

        if (parsedLoggingPolicy is not null)
        {
            // Valid tenant: use the full builder (console + file + seq) with tenant's logging config.
            builderToUse = _loggerConfigurationBuilder;
        }
        else
        {
            // No tenant: build a console-only configuration.
            builderToUse = new TenantLoggerConfigurationBuilder(
                new ILoggingSinkConfigurator[]
                {
                    new ConsoleSinkConfigurator()
                });
        }

        var loggerConfig = builderToUse.Build(tenantId, parsedLoggingPolicy);

        return new CachedLogger(
            loggerConfig.CreateLogger(),
            DateTime.UtcNow);
    }

    private TenantLoggingPolicy ParseLoggingPolicy(Dictionary<string, string?> secrets)
    {
        return new TenantLoggingPolicy
        {
            Enabled = bool.TryParse(secrets.GetValueOrDefault("logging:enabled"), out var enabled) && enabled,
            File = new TenantFileLoggingPolicy
            {
                LogLevel = secrets.GetValueOrDefault("logging:File:logLevel") ?? "Information",
                LogPath = secrets.GetValueOrDefault("logging:File:logPath") ?? "logs/tenant.log",
                RollingInterval = secrets.GetValueOrDefault("logging:File:rollingInterval") ?? "Day",
                RetainedFileCountLimit = int.TryParse(secrets.GetValueOrDefault("logging:File:retainedFileCountLimit"), out var retainedLimit) ? retainedLimit : 7
            },
            Seq = new TenantSeqLoggingPolicy
            {
                LogLevel = secrets.GetValueOrDefault("logging:seq:logLevel") ?? "Information",
                Url = secrets.GetValueOrDefault("logging:seq:url") ?? string.Empty,
                ApiKey = secrets.GetValueOrDefault("logging:seq:apiKey") ?? string.Empty
            }
        };
    }

    private sealed record CachedLogger(
        ILogger Logger,
        DateTime CreatedAt);
}