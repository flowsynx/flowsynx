using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantEmailInvalidException : DomainException
{
    public TenantEmailInvalidException(string email)
        : base(
            DomainErrorCodes.TenantEmailInvalid,
            $"Tenant email is invalid: {email}"
        )
    {
    }
}