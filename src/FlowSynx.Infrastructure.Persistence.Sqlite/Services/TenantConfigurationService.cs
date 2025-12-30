using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public class TenantConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<TenantConfigurationService> _logger;
    private readonly IMemoryCache _cache;

    private const string CONFIG_CACHE_KEY = "TenantConfig_{0}";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

    public TenantConfigurationService(
        IConfiguration configuration,
        ITenantService tenantService,
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        IMemoryCache cache,
        ILogger<TenantConfigurationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private string GetCacheKey(Guid tenantId)
        => string.Format(CONFIG_CACHE_KEY, tenantId);

    private async Task<Dictionary<string, object>> LoadTenantConfigurationAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(tenantId);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CACHE_DURATION;

            var config = new Dictionary<string, object>();

            // 1. Load tenant entity configuration
            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            
            // Fix for CS0019: Convert tenantId (string) to Guid before comparison
            var tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant != null)
            {
                // Add tenant properties as configuration
                config["Tenant:Name"] = tenant.Name;
            }

            // 2. Load tenant settings from database
            var settings = await context.TenantConfigurations
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();

            foreach (var setting in settings)
            {
                config[setting.Key] = setting.GetTypedValue();
            }

            _logger.LogInformation("Loaded configuration for tenant {TenantId} with {Count} entries",
                tenantId, config.Count);

            return config;
        });
    }

    public async Task<T> GetValue<T>(string key, T defaultValue = default, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetCurrentTenantAsync();
        if (tenant == null)
        {
            // Fall back to app configuration if no tenant context
            return _configuration.GetValue(key, defaultValue);
        }

        var config = await LoadTenantConfigurationAsync(tenant.Id, cancellationToken);

        // Check tenant configuration first
        if (config.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // Fallback 1: Check nested tenant configuration
        if (key.StartsWith("Tenant:"))
        {
            var tenantKey = key.Substring("Tenant:".Length);
            if (config.TryGetValue($"Tenant:{tenantKey}", out var tenantValue))
            {
                try
                {
                    return (T)Convert.ChangeType(tenantValue, typeof(T));
                }
                catch
                {
                    // Continue to next fallback
                }
            }
        }

        // Fallback 2: Check app configuration
        var appConfigValue = _configuration.GetValue<T>(key);
        if (appConfigValue != null)
        {
            return appConfigValue;
        }

        return defaultValue;
    }

    public async Task<Dictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetCurrentTenantAsync();
        if (tenant == null)
            return new Dictionary<string, string>();

        var config = await LoadTenantConfigurationAsync(tenant.Id, cancellationToken);
        var result = new Dictionary<string, string>();

        foreach (var kvp in config)
        {
            result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
        }

        return result;
    }

    public async Task UpdateAsync(string key, object value, string userId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantService.GetCurrentTenantAsync();
        if (tenant == null)
            throw new InvalidOperationException("No tenant context");

        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var setting = await context.TenantConfigurations
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Key == key);

        if (setting == null)
        {
            setting = new TenantConfiguration
            {
                TenantId = tenant.Id,
                Key = key
            };
            context.TenantConfigurations.Add(setting);
        }

        setting.Value = value.ToString();
        setting.ValueType = value switch
        {
            int => "int",
            bool => "bool",
            decimal => "decimal",
            _ => "string"
        };

        await context.SaveChangesAsync();

        // Clear cache for this tenant
        var cacheKey = GetCacheKey(tenant.Id);
        _cache.Remove(cacheKey);

        _logger.LogInformation("Updated configuration key {Key} for tenant {TenantId} by user {UserId}",
            key, tenant.Id, userId);
    }
}