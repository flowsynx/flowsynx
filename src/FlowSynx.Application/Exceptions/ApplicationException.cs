using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Application.Exceptions;

public abstract class ApplicationException : BaseException
{
    protected ApplicationException(ErrorCode errorCode, string message) : base(errorCode, message)
    {
    }
}