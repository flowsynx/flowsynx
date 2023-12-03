namespace FlowSync.Abstractions.Exceptions;

public class StorageException : FlowSyncBaseException
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception inner) : base(message, inner) { }
}