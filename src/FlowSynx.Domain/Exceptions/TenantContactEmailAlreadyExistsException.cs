using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantContactEmailAlreadyExistsException : DomainException
{
    public TenantContactEmailAlreadyExistsException(string email)
        : base(
            DomainErrorCodes.TenantContactEmailAlreadyExists,
            $"Contact with email {email} already exists"
        )
    {
    }
}