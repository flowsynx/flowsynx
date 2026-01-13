using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public abstract class RuntimeException : BaseException
{
    protected RuntimeException(ErrorCode errorCode, string message, Exception? innerException = null) 
        : base(errorCode, message, innerException)
    {
    }
}