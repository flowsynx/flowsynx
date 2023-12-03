namespace FlowSync.Abstractions.Exceptions;

public class MessagingException : FlowSyncBaseException
{
    public MessagingException(string message) : base(message) { }
    public MessagingException(string message, Exception inner) : base(message, inner) { }
}