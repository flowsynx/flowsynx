namespace FlowSync.Core.Exceptions;

public class FileSystemException : FlowSyncBaseException
{
    public FileSystemException(string message) : base(message) { }
    public FileSystemException(string message, Exception inner) : base(message, inner) { }
}