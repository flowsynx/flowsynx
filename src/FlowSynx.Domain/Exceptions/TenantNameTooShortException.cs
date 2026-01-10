using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantNameTooShortException : DomainException
{
    public TenantNameTooShortException()
        : base(
            DomainErrorCodes.TenantNameTooShort,
            $"Tenant name must be at least 2 characters long"
        )
    {
    }
}