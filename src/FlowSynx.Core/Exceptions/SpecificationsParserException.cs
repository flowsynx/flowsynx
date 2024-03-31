using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Core.Exceptions;

public class SpecificationsParserException : FlowSynxException
{
    public SpecificationsParserException(string message) : base(message) { }
    public SpecificationsParserException(string message, Exception inner) : base(message, inner) { }
}