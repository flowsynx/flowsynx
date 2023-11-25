namespace FlowSync.Core.Exceptions;

public class SizeParserException : FlowSyncBaseException
{
    public SizeParserException(string message) : base(message) { }
    public SizeParserException(string message, Exception inner) : base(message, inner) { }
}