using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Logging;
using FlowSynx.Infrastructure.Logging.ConsoleLogger;
using FlowSynx.Infrastructure.Logging.FileLogger;
using FlowSynx.Infrastructure.Logging.SeqLogger;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
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
        TenantLoggingPolicy parsedLoggingPolicy = secrets.GetLoggingPolicy();

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
}