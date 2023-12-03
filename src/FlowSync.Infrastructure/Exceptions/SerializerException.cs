using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class SerializerException : FlowSyncBaseException
{
    public SerializerException(string message) : base(message) { }
    public SerializerException(string message, Exception inner) : base(message, inner) { }
}