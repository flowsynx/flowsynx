using FlowSynx.Infrastructure.Serializations.Json.Errors;

namespace FlowSynx.Infrastructure.Serializations.Json.Exceptions;

public sealed class JsonSerializationInputRequiredException : SerializationException
{
    public JsonSerializationInputRequiredException(Type type)
        : base(
            SerializationErrorCodes.JsonSerializationInputRequired,
            $"Input JSON string is null or empty for type {type.Name}."
        )
    {
    }
}