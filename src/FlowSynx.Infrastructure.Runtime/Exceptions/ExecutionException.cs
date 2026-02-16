using FlowSynx.BuildingBlocks.Errors;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public sealed class ExecutionException : RuntimeException
{
    public ExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class RecoverableExecutionException : RuntimeException
{
    public RecoverableExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class WorkflowExecutionException : RuntimeException
{
    public WorkflowExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class WorkflowApplicationExecutionException : RuntimeException
{
    public WorkflowApplicationExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class CyclicDependencyException : RuntimeException
{
    public CyclicDependencyException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}