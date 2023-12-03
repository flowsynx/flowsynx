using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class NamespaceParserException : FlowSyncBaseException
{
    public NamespaceParserException(string message) : base(message) { }
    public NamespaceParserException(string message, Exception inner) : base(message, inner) { }
}