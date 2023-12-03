using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class DeserializerException : FlowSyncBaseException
{
    public DeserializerException(string message) : base(message) { }
    public DeserializerException(string message, Exception inner) : base(message, inner) { }
}