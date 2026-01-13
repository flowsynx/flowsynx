using FlowSynx.BuildingBlocks.Errors;
using FlowSynx.BuildingBlocks.Exceptions;

namespace FlowSynx.Infrastructure.Persistence.Abstractions.Exceptions;

public abstract class PersistenceException : BaseException
{
    protected PersistenceException(ErrorCode errorCode, string message, Exception? innerException = null) 
        : base(errorCode, message, innerException)
    {
    }
}