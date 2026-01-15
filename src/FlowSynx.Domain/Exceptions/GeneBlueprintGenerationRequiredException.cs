using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneGenerationRequiredException : DomainException
{
    public GeneGenerationRequiredException()
        : base(
            DomainErrorCodes.GeneGenerationRequired,
            "Gene generation is required"
        )
    {
    }
}