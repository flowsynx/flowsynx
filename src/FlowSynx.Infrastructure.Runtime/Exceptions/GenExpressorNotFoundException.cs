using FlowSynx.Infrastructure.Runtime.Errors;
using FlowSynx.Infrastructure.Runtime.Exceptions;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public sealed class GenExpressorNotFoundException : RuntimeException
{
    public GenExpressorNotFoundException(string expressorType)
        : base(
            RuntimeErrorCodes.GenExpressorNotFound,
            $"No expressor found for gene type: {expressorType}"
        )
    {
    }
}