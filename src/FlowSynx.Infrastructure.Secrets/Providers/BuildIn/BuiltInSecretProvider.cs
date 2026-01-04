using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Infrastructure.Secrets.Providers.BuildIn;

public class BuiltInSecretProvider : BaseSecretProvider
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IValidateDatabaseConnection _validateDatabaseConnection;
    //private readonly IDataProtector _dataProtector;

    public BuiltInSecretProvider(
        TenantId tenantId,
        IServiceProvider serviceProvider)
        : base(tenantId, ProviderConfiguration.Create(new Dictionary<string, string>()), serviceProvider)
    {
        _tenantRepository = serviceProvider.GetRequiredService<ITenantRepository>();
        _validateDatabaseConnection = serviceProvider.GetRequiredService<IValidateDatabaseConnection>();
        //_dataProtector = serviceProvider.GetRequiredService<IDataProtectionProvider>()
        //.CreateProtector($"TenantSecrets.{tenantId}");
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

        return secret.Value.Value;

        //return secret.Value.IsEncrypted
        //    ? _dataProtector.Unprotect(secret.Value.Value)
        //    : secret.Value.Value;
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
                s => s.Value.Value);

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

        var protectedValue = secretValue.Value;

        //var protectedValue = secretValue.IsEncrypted
        //    ? _dataProtector.Protect(secretValue.Value)
        //    : secretValue.Value;

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