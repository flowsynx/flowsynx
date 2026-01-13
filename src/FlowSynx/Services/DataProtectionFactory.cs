using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace FlowSynx.Services;

public sealed class DataProtectionFactory : IDataProtectionFactory
{
    private readonly IDataProtectionProvider _provider;

    public DataProtectionFactory(IDataProtectionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public IDataProtectionService CreateTenantSecretsService(TenantId tenantId)
    {
        var protector = _provider.CreateProtector(
            $"FlowSynx.Security.TenantSecrets.{tenantId.Value}.v1");
        return new DataProtectionService(protector);
    }

    public IDataProtectionService CreateApiKeyService()
    {
        var protector = _provider.CreateProtector("FlowSynx.Security.ApiKeys.v1");
        return new DataProtectionService(protector);
    }

    public IDataProtectionService CreateRefreshTokenService()
    {
        var protector = _provider.CreateProtector("FlowSynx.Security.RefreshTokens.v1");
        return new DataProtectionService(protector);
    }
}