using FlowSynx.Application.Errors;

namespace FlowSynx.Application.Exceptions;

public sealed class TenantAlreadyExistException : ApplicationException
{
    public TenantAlreadyExistException(string tenantName)
        : base(
            ApplicationErrorCodes.TenantAlreadyExists,
            $"The tenant with name '{tenantName}' already exists."
        )
    {
    }
}