namespace FlowSync.Core.Exceptions;

public class SizeParserException : Exception
{
    public SizeParserException() { }
    public SizeParserException(string message) : base(message) { }
    public SizeParserException(string message, Exception inner) : base(message, inner) { }
}