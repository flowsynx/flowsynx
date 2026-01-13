using FlowSynx.Infrastructure.Persistence.Abstractions.Errors;

namespace FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;

public sealed class AuditingApplyException : PersistenceException
{
    public AuditingApplyException(Exception exception)
        : base(
            PersistenceErrorCodes.AuditsApplying,
            "Failed to apply auditing information to tracked entities.",
            exception
        )
    {
    }
}