using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Exceptions;

public abstract class SystemException : BaseException
{
    protected SystemException(ErrorCode errorCode, string message, Exception? innerException = null) 
        : base(errorCode, message, innerException)
    {
    }
}