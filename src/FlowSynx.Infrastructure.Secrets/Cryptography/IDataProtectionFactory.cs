using FlowSynx.Domain.Tenants;

namespace FlowSynx.Infrastructure.Security.Cryptography;

public interface IDataProtectionFactory
{
    IDataProtectionService CreateTenantSecretsService(TenantId tenantId);
}
