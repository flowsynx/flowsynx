using FlowSynx.Application;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Infrastructure.Logging;

public sealed class SerilogTenantLoggerFactory : ITenantLoggerFactory
{
    private readonly ITenantRepository _configProvider;
    private readonly ConcurrentDictionary<TenantId, CachedLogger> _cache = new();

    public SerilogTenantLoggerFactory(
        ITenantRepository configProvider)
    {
        _configProvider = configProvider;
    }

    public ILogger GetLogger(TenantId tenantId)
    {
        var cached = _cache.GetOrAdd(tenantId, _ => CreateLogger(tenantId).GetAwaiter().GetResult());

        return cached.Logger;
    }

    private async Task<CachedLogger> CreateLogger(TenantId tenantId)
    {
        var filePath = await _configProvider.GetConfigurationValueAsync<string>(tenantId, "Logger:FilePath");
        var minimumLevel = await _configProvider.GetConfigurationValueAsync<string>(tenantId, "Logger:MinimumLevel");

        var loggerConfig = new LoggerConfiguration()
            //.MinimumLevel.Is(minimumLevel)
            .Enrich.WithProperty("TenantId", tenantId);

        //if (!string.IsNullOrWhiteSpace(filePath))
        //{
            var logPath = Path.Combine("logs", $"tenant-{tenantId}", "log-.txt");

            loggerConfig = loggerConfig.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                shared: true);
        //}

        //if (!string.IsNullOrWhiteSpace(seqUrl))
        //{
        //    loggerConfig = loggerConfig.WriteTo.Seq(
        //        serverUrl: seqUrl,
        //        apiKey: seqApiKey);
        //}

        return new CachedLogger(
            loggerConfig.CreateLogger(),
            DateTime.UtcNow);
    }

    private sealed record CachedLogger(
        ILogger Logger,
        DateTime CreatedAt);
}
