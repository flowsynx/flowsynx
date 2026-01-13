using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Security.Errors;

namespace FlowSynx.Infrastructure.Security.Exceptions;

public sealed class NoSecretProviderException : SecurityException
{
    public NoSecretProviderException(TenantId tenantId)
        : base(
            SecurityErrorCodes.NoSecretProviderFound,
            $"No secret provider configured for tenant '{tenantId}'"
        )
    {
    }
}