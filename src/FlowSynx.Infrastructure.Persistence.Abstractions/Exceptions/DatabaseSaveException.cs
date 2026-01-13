using FlowSynx.Infrastructure.Persistence.Abstractions.Errors;

namespace FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;

public sealed class DatabaseSaveException : PersistenceException
{
    public DatabaseSaveException(Exception exception)
        : base(
            PersistenceErrorCodes.DatabaseSaveData,
            "Failed to save changes to the database.",
            exception
        )
    {
    }
}