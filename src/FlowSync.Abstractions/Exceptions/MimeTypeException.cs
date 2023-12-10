namespace FlowSync.Abstractions.Exceptions;

public class MimeTypeException : FlowSyncBaseException
{
    public MimeTypeException(string message) : base(message) { }
    public MimeTypeException(string message, Exception inner) : base(message, inner) { }
}