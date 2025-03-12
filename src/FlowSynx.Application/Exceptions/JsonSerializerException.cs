using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Application.Exceptions;

public class JsonSerializerException : BaseException
{
    public JsonSerializerException(string message) : base(message) { }
    public JsonSerializerException(string message, Exception inner) : base(message, inner) { }
}