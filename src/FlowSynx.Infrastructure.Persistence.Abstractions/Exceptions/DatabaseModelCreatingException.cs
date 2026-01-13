using FlowSynx.Infrastructure.Persistence.Abstractions.Errors;

namespace FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;

public sealed class DatabaseModelCreatingException : PersistenceException
{
    public DatabaseModelCreatingException(Exception exception)
        : base(
            PersistenceErrorCodes.DatabaseModelCreating,
            "An error occurred while building the EF Core model.",
            exception
        )
    {
    }
}