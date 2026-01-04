using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public abstract class BaseSecretProvider : ISecretProvider
{
    protected readonly TenantId _tenantId;
    protected readonly ProviderConfiguration _configuration;
    protected readonly IMemoryCache _cache;
    protected readonly ILogger _logger;
    protected readonly IServiceProvider _serviceProvider;

    protected BaseSecretProvider(
        TenantId tenantId,
        ProviderConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _tenantId = tenantId;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _cache = serviceProvider.GetRequiredService<IMemoryCache>();
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType().Name);
    }

    public abstract SecretProviderType ProviderType { get; }

    public virtual async Task<string?> GetSecretAsync(SecretKey secretKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"secret_{_tenantId}_{secretKey.Value}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await GetSecretInternalAsync(secretKey, cancellationToken);
        });
    }

    protected abstract Task<string?> GetSecretInternalAsync(SecretKey secretKey, CancellationToken cancellationToken = default);

    public abstract Task<Dictionary<string, string?>> GetSecretsAsync(string? prefix = null, CancellationToken cancellationToken = default);
    public abstract Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
    public abstract Task SetSecretAsync(SecretKey secretKey, SecretValue secretValue, CancellationToken cancellationToken = default);
}