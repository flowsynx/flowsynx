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

public sealed class ChromosomeExecutionException : RuntimeException
{
    public ChromosomeExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException)
    {
    }
}

public sealed class GenomeExecutionException : RuntimeException
{
    public GenomeExecutionException(ErrorCode errorCode, string message, Exception? innerException = null)
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