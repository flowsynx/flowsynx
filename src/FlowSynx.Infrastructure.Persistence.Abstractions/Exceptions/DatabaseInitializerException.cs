using FlowSynx.Infrastructure.Persistence.Abstractions.Errors;

namespace FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;

public sealed class DatabaseInitializerException : PersistenceException
{
    public DatabaseInitializerException(Exception exception)
        : base(
            PersistenceErrorCodes.DatabaseInitializer,
            $"Error occurred while connecting the application database: {exception.Message}",
            exception
        )
    {
    }
}