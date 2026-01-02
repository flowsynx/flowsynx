using FlowSynx.Application.Core.Interfaces;
using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Logging.ConsoleLogger;
using FlowSynx.Infrastructure.Logging.FileLogger;
using FlowSynx.Infrastructure.Logging.SeqLogger;
using Serilog;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Logging;

public sealed class SerilogTenantLoggerFactory : ITenantLoggerFactory
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILoggerConfigurationBuilder _loggerConfigurationBuilder;
    private readonly ConcurrentDictionary<TenantId, CachedLogger> _cache = new();

    public SerilogTenantLoggerFactory(ITenantRepository tenantRepository)
        : this(tenantRepository, new TenantLoggerConfigurationBuilder(
            new ILoggingSinkConfigurator[]
            {
                new ConsoleSinkConfigurator(),
                new FileSinkConfigurator(),
                new SeqSinkConfigurator()
            }))
    {
    }

    internal SerilogTenantLoggerFactory(
        ITenantRepository tenantRepository,
        ILoggerConfigurationBuilder loggerConfigurationBuilder)
    {
        _tenantRepository = tenantRepository;
        _loggerConfigurationBuilder = loggerConfigurationBuilder;
    }

    public ILogger GetLogger(TenantId tenantId)
    {
        var cached = _cache.GetOrAdd(tenantId, CreateLoggerAsync(tenantId).GetAwaiter().GetResult());
        return cached.Logger;
    }

    private async Task<CachedLogger> CreateLoggerAsync(TenantId tenantId)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, CancellationToken.None);

        // Always allow console logging. Enable file/seq only when tenant is valid.
        ILoggerConfigurationBuilder builderToUse;

        if (tenant is not null)
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

        var loggerConfig = builderToUse.Build(tenantId, tenant?.Configuration.Logging);

        return new CachedLogger(
            loggerConfig.CreateLogger(),
            DateTime.UtcNow);
    }

    private sealed record CachedLogger(
        ILogger Logger,
        DateTime CreatedAt);
}