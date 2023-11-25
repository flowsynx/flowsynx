namespace FlowSync.Core.Exceptions;

public class RemotePathParserException : Exception
{
    public RemotePathParserException() { }
    public RemotePathParserException(string message) : base(message) { }
    public RemotePathParserException(string message, Exception inner) : base(message, inner) { }
}