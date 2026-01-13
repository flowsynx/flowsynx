using FlowSynx.Infrastructure.Serializations.Json.Errors;

namespace FlowSynx.Infrastructure.Serializations.Json.Exceptions;

public sealed class JsonSerializationException : SerializationException
{
    public JsonSerializationException(string message)
    : base(
        SerializationErrorCodes.JsonSerializationFailed,
        message
    )
    {
    }

    public JsonSerializationException(string message, Exception ex)
        : base(
            SerializationErrorCodes.JsonSerializationFailed,
            message,
            ex
        )
    {
    }
}