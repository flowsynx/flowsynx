namespace FlowSync.Core.Exceptions;

public class DateParserException : Exception
{
    public DateParserException() { }
    public DateParserException(string message) : base(message) { }
    public DateParserException(string message, Exception inner) : base(message, inner) { }
}