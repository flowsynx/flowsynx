namespace FlowSync.Abstractions.Exceptions;

public class FlowSyncBaseException : Exception
{
    protected FlowSyncBaseException(string message)
        : base(message)
    {
    }

    protected FlowSyncBaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}