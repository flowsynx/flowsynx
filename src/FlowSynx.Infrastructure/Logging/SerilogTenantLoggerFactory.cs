using FlowSynx.Application;
using FlowSynx.Domain.Tenants;
using Serilog;
using System.Collections.Concurrent;

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
        var config = await _configProvider.GetByIdAsync(tenantId, CancellationToken.None);

        var loggerConfig = new LoggerConfiguration()
            //.MinimumLevel.Is(minimumLevel)
            .Enrich.WithProperty("TenantId", tenantId);

        //if (!string.IsNullOrWhiteSpace(filePath))
        //{
            var filePath = config.Configuration.Logging.File.LogPath; // e.g., "logs/tenant-{tenantId}/log-.txt"
            var logPath = Path.Combine(filePath, $"tenant-{tenantId}", "log-.txt");

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
