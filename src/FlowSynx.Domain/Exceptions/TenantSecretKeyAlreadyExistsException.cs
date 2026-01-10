using FlowSynx.Domain.Errors;
using FlowSynx.Domain.TenantSecrets;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantSecretKeyAlreadyExistsException : DomainException
{
    public TenantSecretKeyAlreadyExistsException(SecretKey key)
        : base(
            DomainErrorCodes.TenantSecretKeyAlreadyExists,
            $"Secret with key '{key.Value}' already exists"
        )
    {
    }
}