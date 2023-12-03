namespace FlowSync.Abstractions.Exceptions;

public class KeyValuesException : FlowSyncBaseException
{
    public KeyValuesException(string message) : base(message) { }
    public KeyValuesException(string message, Exception inner) : base(message, inner) { }
}