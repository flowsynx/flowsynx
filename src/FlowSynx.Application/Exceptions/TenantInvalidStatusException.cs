using FlowSynx.Application.Errors;

namespace FlowSynx.Application.Exceptions;

public sealed class TenantInvalidStatusException : ApplicationException
{
    public TenantInvalidStatusException(string status)
        : base(
            ApplicationErrorCodes.TenantInvalidStatus,
            $"The tenant has an invalid status: {status}."
        )
    {
    }

    public TenantInvalidStatusException(Guid tenantId, string status)
        : base(
            ApplicationErrorCodes.TenantInvalidStatus,
            $"The tenant with id '{tenantId}' has an invalid status: {status}."
        )
    {
    }

    public TenantInvalidStatusException(string tenantName, string status)
    : base(
        ApplicationErrorCodes.TenantInvalidStatus,
        $"The tenant '{tenantName}' has an invalid status: {status}."
    )
    {
    }
}