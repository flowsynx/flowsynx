using FlowSynx.Application.Errors;

namespace FlowSynx.Application.Exceptions;

public sealed class AuditTrailAuditNotFoundException : ApplicationException
{
    public AuditTrailAuditNotFoundException(long auditTrailId)
        : base(
            ApplicationErrorCodes.AuditTrailNotFound,
            $"The audit details, id '{auditTrailId}', are not found."
        )
    {
    }
}