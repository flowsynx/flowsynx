using FlowSynx.Infrastructure.Runtime.Errors;

namespace FlowSynx.Infrastructure.Runtime.Exceptions;

public sealed class GeneNotFoundException : RuntimeException
{
    public GeneNotFoundException(Guid geneId)
        : base(
            RuntimeErrorCodes.GeneNotFound,
            $"Gene not found: {geneId}"
        )
    {
    }
}