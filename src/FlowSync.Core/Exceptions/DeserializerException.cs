namespace FlowSync.Core.Exceptions;

public class DeserializerException : Exception
{
    public DeserializerException() { }
    public DeserializerException(string message) : base(message) { }
    public DeserializerException(string message, Exception inner) : base(message, inner) { }
}