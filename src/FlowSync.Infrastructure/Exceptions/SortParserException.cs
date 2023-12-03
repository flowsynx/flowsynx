using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Infrastructure.Exceptions;

public class SortParserException : FlowSyncBaseException
{
    public SortParserException(string message) : base(message) { }
    public SortParserException(string message, Exception inner) : base(message, inner) { }
}