using FlowSynx.Domain.Exceptions;

namespace FlowSynx.IO.Exceptions;

public class JsonDeserializerException : BaseException
{
    public JsonDeserializerException(string message) : base(message) { }
    public JsonDeserializerException(string message, Exception inner) : base(message, inner) { }
}