using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Core.Exceptions;

public class StorageNormsParserException : FlowSynxException
{
    public StorageNormsParserException(string message) : base(message) { }
    public StorageNormsParserException(string message, Exception inner) : base(message, inner) { }
}