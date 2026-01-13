using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class TenantNameTooLongException : DomainException
{
    public TenantNameTooLongException()
        : base(
            DomainErrorCodes.TenantNameTooLong,
            "Tenant name cannot exceed 100 characters"
        )
    {
    }
}