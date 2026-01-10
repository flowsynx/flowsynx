using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Domain.Exceptions;

public abstract class DomainException : BaseException
{
    protected DomainException(ErrorCode errorCode, string message) : base(errorCode, message)
    {
    }
}