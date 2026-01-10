using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Infrastructure.Security.Errors;

namespace FlowSynx.Infrastructure.Security.Exceptions;

public sealed class UnsupportedSecretProviderException : SecurityException
{
    public UnsupportedSecretProviderException(SecretProviderType secretProviderType)
        : base(
            SecurityErrorCodes.NoSecretProviderFound,
            $"Unsupported provider type: '{secretProviderType}'"
        )
    {
    }
}