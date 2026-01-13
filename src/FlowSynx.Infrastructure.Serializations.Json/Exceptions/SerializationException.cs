using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Infrastructure.Serializations.Json.Exceptions;

public abstract class SerializationException : BaseException
{
    protected SerializationException(ErrorCode errorCode, string message, Exception? innerException = null) 
        : base(errorCode, message, innerException)
    {
    }
}