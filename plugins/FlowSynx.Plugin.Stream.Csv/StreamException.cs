using FlowSynx.Abstractions.Exceptions;

namespace FlowSynx.Plugin.Stream.Csv;

public class StreamException : FlowSynxException
{
    public StreamException(string message) : base(message) { }
    public StreamException(string message, Exception inner) : base(message, inner) { }
}