using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class StorageNormsParserException : FlowSyncBaseException
{
    public StorageNormsParserException(string message) : base(message) { }
    public StorageNormsParserException(string message, Exception inner) : base(message, inner) { }
}