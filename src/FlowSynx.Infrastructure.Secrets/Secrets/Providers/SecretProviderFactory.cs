using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Infrastructure.Security.Secrets.Exceptions;
using FlowSynx.Infrastructure.Security.Secrets.Providers.BuildIn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Security.Secrets.Providers;

public class SecretProviderFactory : ISecretProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretProviderFactory> _logger;
    private readonly ConcurrentDictionary<string, ISecretProvider> _providers = new();

    public SecretProviderFactory(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<SecretProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ISecretProvider> GetProviderForTenantAsync(TenantId tenantId, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepository = scope.ServiceProvider.GetRequiredService<ITenantSecretConfigRepository>();
        var config = await configRepository.GetByTenantIdAsync(tenantId, ct)
                     ?? throw new SecretException($"No secret provider configured for tenant {tenantId}");

        return CreateProvider(tenantId, config.ProviderType, config.Configuration, scope.ServiceProvider);
    }

    public ISecretProvider CreateProvider(
        TenantId tenantId,
        SecretProviderType providerType,
        ProviderConfiguration configuration,
        IServiceProvider scopedProvider)
    {
        return providerType switch
        {
            SecretProviderType.BuiltIn => new BuiltInSecretProvider(tenantId, scopedProvider),
            //SecretProviderType.AzureKeyVault => new AzureKeyVaultProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.AwsSecretsManager => new AwsSecretsManagerProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.HashiCorpVault => new HashiCorpVaultProvider(tenantId, configuration, _serviceProvider),
            //SecretProviderType.Infisical => new InfisicalProvider(tenantId, configuration, _serviceProvider),
            _ => throw new DomainException($"Unsupported provider type: {providerType}")
        };
    }
}