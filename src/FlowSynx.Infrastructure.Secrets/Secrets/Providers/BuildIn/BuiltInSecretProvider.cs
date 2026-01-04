using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using FlowSynx.Infrastructure.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Security.Secrets.Providers.BuildIn;

public class BuiltInSecretProvider : BaseSecretProvider
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IValidateDatabaseConnection _validateDatabaseConnection;
    private readonly IDataProtectionService _dataProtectionService;

    public BuiltInSecretProvider(
    TenantId tenantId,
    IServiceProvider serviceProvider)
    : base(tenantId, ProviderConfiguration.Create(new Dictionary<string, string>()), serviceProvider)
    {
        _tenantRepository = serviceProvider.GetRequiredService<ITenantRepository>() 
            ?? throw new ArgumentNullException(nameof(ITenantRepository));

        _validateDatabaseConnection = serviceProvider.GetRequiredService<IValidateDatabaseConnection>() 
            ?? throw new ArgumentNullException(nameof(IValidateDatabaseConnection));

        _dataProtectionService = serviceProvider.GetRequiredService<IDataProtectionFactory>()?.CreateTenantSecretsService(tenantId)
            ?? throw new ArgumentNullException(nameof(IDataProtectionFactory));
    }

    public override SecretProviderType ProviderType => SecretProviderType.BuiltIn;

    protected override async Task<string?> GetSecretInternalAsync(SecretKey secretKey, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetWithSecretsAsync(_tenantId, cancellationToken);

        if (tenant == null)
            throw new DomainException($"Tenant {_tenantId} not found");

        var secret = tenant.GetSecret(secretKey);
        if (secret == null || secret.IsExpired)
            return null;

        return secret.Value.IsEncrypted
            ? _dataProtectionService.Unprotect(secret.Value.Value)
            : secret.Value.Value;
    }

    public override async Task<Dictionary<string, string?>> GetSecretsAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetWithSecretsAsync(_tenantId, cancellationToken);

        if (tenant == null)
            throw new DomainException($"Tenant {_tenantId} not found");

        var secrets = tenant.Secrets
            .Where(s => string.IsNullOrEmpty(prefix) || s.Key.Value.StartsWith(prefix))
            .Where(s => !s.IsExpired)
            .ToDictionary(
                s => s.Key.Value,
                s => s.Value.IsEncrypted
                    ? _dataProtectionService.Unprotect(s.Value.Value)
                    : s.Value.Value);

        return secrets;
    }

    public override async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _validateDatabaseConnection.ValidateConnection(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override async Task SetSecretAsync(SecretKey secretKey, SecretValue secretValue, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetWithSecretsAsync(_tenantId, cancellationToken);

        if (tenant == null)
            throw new DomainException($"Tenant {_tenantId} not found");

        var protectedValue = secretValue.IsEncrypted
            ? _dataProtectionService.Protect(secretValue.Value)
            : secretValue.Value;

        var newSecretValue = SecretValue.Create(protectedValue, secretValue.IsEncrypted, secretValue.ExpiresAt);

        var existing = tenant.GetSecret(secretKey);
        if (existing != null)
        {
            existing.UpdateValue(newSecretValue);
        }
        else
        {
            tenant.AddSecret(secretKey, newSecretValue);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
    }
}