using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Core.Exceptions;

public class NamespaceParserException : FlowSynxException
{
    public NamespaceParserException(string message) : base(message) { }
    public NamespaceParserException(string message, Exception inner) : base(message, inner) { }
}