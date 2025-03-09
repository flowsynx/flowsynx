using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Connectors.Database.MySql.Exceptions;

public class DatabaseException : FlowSynxException
{
    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception inner) : base(message, inner) { }
}
