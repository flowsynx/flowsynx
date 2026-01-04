using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Infrastructure.Secrets.Providers.BuildIn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Secrets.Providers;

public class SecretProviderFactory : ISecretProviderFactory
{
    private readonly ITenantSecretConfigRepository _configRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretProviderFactory> _logger;
    private readonly ConcurrentDictionary<string, ISecretProvider> _providers = new();

    public SecretProviderFactory(
        ITenantSecretConfigRepository configRepository,
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<SecretProviderFactory> logger)
    {
        _configRepository = configRepository;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ISecretProvider> GetProviderForTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"provider_{tenantId}";

        if (_providers.TryGetValue(cacheKey, out var provider))
            return provider;

        var config = await _configRepository.GetByTenantIdAsync(tenantId, ct);

        if (config == null)
            throw new SecretException($"No secret provider configured for tenant {tenantId}");

        provider = CreateProvider(tenantId, config.ProviderType, config.Configuration);
        _providers[cacheKey] = provider;

        return provider;
    }

    public ISecretProvider CreateProvider(TenantId tenantId, SecretProviderType providerType, ProviderConfiguration configuration)
    {
        return providerType switch
        {
            SecretProviderType.BuiltIn => new BuiltInSecretProvider(tenantId, _serviceProvider),
            //SecretProviderType.AzureKeyVault => new AzureKeyVaultProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.AwsSecretsManager => new AwsSecretsManagerProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.HashiCorpVault => new HashiCorpVaultProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.Infisical => new InfisicalProvider(tenantId, configuration, _serviceProvider),
            _ => throw new DomainException($"Unsupported provider type: {providerType}")
        };
    }
}